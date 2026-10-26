using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>
    /// شاشة سجل وأرشيف طلبيات وأوامر الشراء ومتابعة حالتها
    /// </summary>
    public class FrmPurchaseOrdersList : Form
    {
        private DateTimePicker dtpFrom, dtpTo;
        private ComboBox cboSupplier, cboStatus;
        private TextBox txtSearch;
        private Button btnRefresh, btnNewOrder, btnEditOrder, btnConvertToPurchase, btnWhatsApp, btnPrint, btnExportExcel, btnCancelOrder, btnDeleteOrder;
        private DataGridView dgvOrders;
        private Label lblTotalOrdersCount, lblTotalOrdersAmount;

        private DataTable _dtOrders;

        public FrmPurchaseOrdersList()
        {
            if (!Session.CanAccess("Purchases") && !Session.CanAccess("PurchaseOrdersList") && !Session.IsAdmin)
            {
                MessageBox.Show("غير مصرح لك بعرض سجل أوامر الشراء.", "تنبيه الصلاحيات", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.Load += (s, e) => this.Close();
                return;
            }

            InitUI();
            LoadSuppliers();
            LoadOrders();
        }

        private void InitUI()
        {
            this.Text = "سجل طلبيات وأوامر الشراء";
            this.Size = new Size(1220, 760);
            this.MinimumSize = new Size(980, 580);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            // 1. ترويسة الشاشة
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Theme.BgCard,
                Padding = new Padding(15, 8, 15, 8)
            };

            var lblTitle = new Label
            {
                Text = "سجل ومتابعة طلبيات وأوامر الشراء (Purchase Orders Archive)",
                Font = new Font("Segoe UI", 12.5f, FontStyle.Bold),
                ForeColor = Theme.TextMain,
                Dock = DockStyle.Top,
                Height = 26
            };

            var lblSub = new Label
            {
                Text = "أرشيف ومتابعة أوامر الشراء المرسلة للموردين، التحقق من التوريد، والتحويل المباشر لفواتير المشتريات",
                Font = new Font("Segoe UI", 8.8f),
                ForeColor = Theme.TextSub,
                Dock = DockStyle.Top,
                Height = 20
            };

            pnlHeader.Controls.Add(lblSub);
            pnlHeader.Controls.Add(lblTitle);

            // 2. شريط الفلاتر
            var pnlFilter = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Theme.BgCard,
                Padding = new Padding(10, 6, 10, 8),
                RightToLeft = RightToLeft.Yes,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true
            };

            dtpFrom = new DateTimePicker { Width = 115, Format = DateTimePickerFormat.Short, Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1) };
            dtpFrom.ValueChanged += (s, e) => LoadOrders();

            dtpTo = new DateTimePicker { Width = 115, Format = DateTimePickerFormat.Short, Value = DateTime.Today };
            dtpTo.ValueChanged += (s, e) => LoadOrders();

            pnlFilter.Controls.Add(MakeFilterItem("من تاريخ:", dtpFrom));
            pnlFilter.Controls.Add(MakeFilterItem("إلى تاريخ:", dtpTo));

            cboSupplier = new ComboBox { Width = 175, DropDownStyle = ComboBoxStyle.DropDownList };
            cboSupplier.SelectedIndexChanged += (s, e) => LoadOrders();
            pnlFilter.Controls.Add(MakeFilterItem("المورد :", cboSupplier));

            cboStatus = new ComboBox { Width = 135, DropDownStyle = ComboBoxStyle.DropDownList };
            cboStatus.Items.AddRange(new object[] { "الكل", "معلقة", "تم التوريد بالكامل", "ملغاة" });
            cboStatus.SelectedIndex = 0;
            cboStatus.SelectedIndexChanged += (s, e) => LoadOrders();
            pnlFilter.Controls.Add(MakeFilterItem("الحالة :", cboStatus));

            txtSearch = new TextBox { Width = 140, Height = 26 };
            txtSearch.TextChanged += (s, e) => ApplyLocalSearch();
            pnlFilter.Controls.Add(MakeFilterItem("بحث:", txtSearch));

            btnRefresh = Theme.MakeButton("تحديث", 0, 0, 85, 30, Color.FromArgb(70, 80, 95));
            btnRefresh.Margin = new Padding(4, 18, 4, 2);
            btnRefresh.Click += (s, e) => LoadOrders();
            pnlFilter.Controls.Add(btnRefresh);

            // 3. شريط أزرار العمليات
            var pnlActions = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Theme.BgCard,
                Padding = new Padding(10, 4, 10, 8),
                RightToLeft = RightToLeft.Yes,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true
            };

            btnNewOrder = Theme.MakeButton("أمر شراء جديد", 0, 0, 150, 34, Color.FromArgb(5, 150, 105));
            btnNewOrder.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnNewOrder.Click += (s, e) => OpenNewOrder();

            btnEditOrder = Theme.MakeButton("عرض وتعديل الطلبية", 0, 0, 165, 34, Color.FromArgb(37, 99, 235));
            btnEditOrder.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnEditOrder.Click += (s, e) => OpenSelectedOrder();

            btnConvertToPurchase = Theme.MakeButton("تحويل لفاتورة مشتريات", 0, 0, 195, 34, Color.FromArgb(234, 88, 12));
            btnConvertToPurchase.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnConvertToPurchase.Click += (s, e) => ConvertSelectedToPurchase();

            btnWhatsApp = Theme.MakeButton("إرسال واتساب", 0, 0, 130, 34, Color.FromArgb(37, 211, 102));
            btnWhatsApp.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnWhatsApp.Click += (s, e) => SendWhatsAppSelected();

            btnPrint = Theme.MakeButton("طباعة A4", 0, 0, 110, 34, Color.FromArgb(100, 116, 139));
            btnPrint.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnPrint.Click += (s, e) => PrintSelected();

            btnExportExcel = Theme.MakeButton("تصدير إكسيل", 0, 0, 130, 34, Color.FromArgb(13, 148, 136));
            btnExportExcel.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnExportExcel.Click += (s, e) => ExportToExcel();

            btnCancelOrder = Theme.MakeButton("إلغاء الطلبية", 0, 0, 120, 34, Color.FromArgb(220, 38, 38));
            btnCancelOrder.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnCancelOrder.Click += (s, e) => CancelSelectedOrder();

            btnDeleteOrder = Theme.MakeButton("حذف نهائي", 0, 0, 115, 34, Color.FromArgb(185, 28, 28));
            btnDeleteOrder.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnDeleteOrder.Click += (s, e) => DeleteSelectedOrder();

            pnlActions.Controls.Add(btnNewOrder);
            pnlActions.Controls.Add(btnEditOrder);
            pnlActions.Controls.Add(btnConvertToPurchase);
            pnlActions.Controls.Add(btnWhatsApp);
            pnlActions.Controls.Add(btnPrint);
            pnlActions.Controls.Add(btnExportExcel);
            pnlActions.Controls.Add(btnCancelOrder);
            pnlActions.Controls.Add(btnDeleteOrder);

            // 4. جدول الطلبيات
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
                RowTemplate = { Height = 34 }
            };

            SetupGridColumns();
            dgvOrders.CellDoubleClick += (s, e) => OpenSelectedOrder();
            dgvOrders.CellFormatting += DgvOrders_CellFormatting;

            // 5. شريط الإحصائيات السفلي
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

            lblTotalOrdersCount = new Label { Text = "0", AutoSize = true, Font = Theme.FontBold, ForeColor = Color.FromArgb(37, 99, 235), Margin = new Padding(2, 6, 30, 0) };
            lblTotalOrdersAmount = new Label { Text = "0.00 ج", AutoSize = true, Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = Color.FromArgb(5, 150, 105), Margin = new Padding(2, 4, 10, 0) };

            flowSum.Controls.Add(new Label { Text = "إجمالي عدد الطلبيات:", AutoSize = true, Font = Theme.FontBold, ForeColor = Theme.TextMain, Margin = new Padding(2, 6, 2, 0) });
            flowSum.Controls.Add(lblTotalOrdersCount);

            flowSum.Controls.Add(new Label { Text = "إجمالي التكلفة التقديرية:", AutoSize = true, Font = Theme.FontBold, ForeColor = Theme.TextMain, Margin = new Padding(2, 6, 2, 0) });
            flowSum.Controls.Add(lblTotalOrdersAmount);

            pnlSummary.Controls.Add(flowSum);

            this.Controls.Add(dgvOrders);
            this.Controls.Add(pnlSummary);
            this.Controls.Add(pnlActions);
            this.Controls.Add(pnlFilter);
            this.Controls.Add(pnlHeader);
        }

        private void SetupGridColumns()
        {
            dgvOrders.Columns.Clear();
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "OrderID", DataPropertyName = "OrderID", HeaderText = "#", Width = 55, Visible = false });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "OrderCode", DataPropertyName = "OrderCode", HeaderText = "كود الطلبية", Width = 140, DefaultCellStyle = { Font = Theme.FontBold, Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "OrderDate", DataPropertyName = "OrderDate", HeaderText = "تاريخ الطلب", Width = 145, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, Format = "yyyy/MM/dd hh:mm tt" } });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "SupplierName", DataPropertyName = "SupplierName", HeaderText = "المورد", Width = 190, DefaultCellStyle = { Font = Theme.FontBold } });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "SupplierPhone", DataPropertyName = "SupplierPhone", HeaderText = "هاتف المورد", Width = 120, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalItemsCount", DataPropertyName = "TotalItemsCount", HeaderText = "عدد الأصناف", Width = 95, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalQuantity", DataPropertyName = "TotalQuantity", HeaderText = "إجمالي الكمية", Width = 105, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, Format = "N2" } });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalEstimatedCost", DataPropertyName = "TotalEstimatedCost", HeaderText = "التكلفة التقديرية", Width = 135, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, Format = "N2", Font = Theme.FontBold, ForeColor = Color.FromArgb(5, 150, 105) } });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", DataPropertyName = "Status", HeaderText = "الحالة", Width = 120, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = Theme.FontBold } });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "ExpectedDeliveryDate", DataPropertyName = "ExpectedDeliveryDate", HeaderText = "تاريخ التوريد المتوقع", Width = 130, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, Format = "yyyy/MM/dd" } });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "CreatedByEmpName", DataPropertyName = "CreatedByEmpName", HeaderText = "المستخدم", Width = 110, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "Notes", DataPropertyName = "Notes", HeaderText = "ملاحظات", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        }

        private void DgvOrders_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= dgvOrders.Rows.Count) return;
            if (!dgvOrders.Columns.Contains("Status")) return;

            var drv = dgvOrders.Rows[e.RowIndex].DataBoundItem as DataRowView;
            if (drv == null) return;

            string status = drv.Row.Table.Columns.Contains("Status") ? (drv["Status"]?.ToString() ?? "") : "";
            var cell = dgvOrders.Rows[e.RowIndex].Cells["Status"];
            if (cell == null) return;

            if (status.Contains("كامل"))
            {
                cell.Style.ForeColor = Color.FromArgb(6, 95, 70);
                cell.Style.BackColor = Color.FromArgb(236, 253, 245);
            }
            else if (status.Contains("معلقة"))
            {
                cell.Style.ForeColor = Color.FromArgb(180, 83, 9);
                cell.Style.BackColor = Color.FromArgb(254, 243, 199);
            }
            else if (status.Contains("ملغاة"))
            {
                cell.Style.ForeColor = Color.FromArgb(185, 28, 28);
                cell.Style.BackColor = Color.FromArgb(254, 226, 226);
            }
        }

        private void LoadSuppliers()
        {
            try
            {
                cboSupplier.Items.Clear();
                cboSupplier.Items.Add(new ComboItem(0, "كل الموردين"));
                var dt = DbHelper.Query("SELECT SupplierID, SupplierName FROM Suppliers WHERE IsActive=1 ORDER BY SupplierName");
                foreach (DataRow r in dt.Rows)
                {
                    cboSupplier.Items.Add(new ComboItem(Convert.ToInt32(r["SupplierID"]), r["SupplierName"].ToString()));
                }
                cboSupplier.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                AppLogger.Error("FrmPurchaseOrdersList.LoadSuppliers", ex);
            }
        }

        private void LoadOrders()
        {
            try
            {
                DateTime f = dtpFrom.Value.Date;
                DateTime t = dtpTo.Value.Date;
                int? supId = (cboSupplier.SelectedItem is ComboItem ci && ci.ID > 0) ? ci.ID : (int?)null;
                string st = cboStatus.SelectedItem?.ToString();

                _dtOrders = PurchaseOrderDAL.GetOrdersList(f, t, supId, st, txtSearch.Text.Trim());
                dgvOrders.DataSource = _dtOrders;

                UpdateSummary();
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء جلب أوامر الشراء:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                AppLogger.Error("FrmPurchaseOrdersList.LoadOrders", ex);
            }
        }

        private void ApplyLocalSearch()
        {
            if (_dtOrders == null) return;
            string s = txtSearch.Text?.Trim().Replace("'", "''") ?? "";
            var dv = _dtOrders.DefaultView;
            if (!string.IsNullOrEmpty(s))
            {
                dv.RowFilter = $"OrderCode LIKE '%{s}%' OR SupplierName LIKE '%{s}%' OR Notes LIKE '%{s}%'";
            }
            else
            {
                dv.RowFilter = "";
            }
            UpdateSummary();
        }

        private void UpdateSummary()
        {
            if (_dtOrders == null) return;
            var dv = _dtOrders.DefaultView;
            int count = dv.Count;
            decimal totalAmt = 0;

            foreach (DataRowView drv in dv)
            {
                if (drv["TotalEstimatedCost"] != DBNull.Value)
                    totalAmt += Convert.ToDecimal(drv["TotalEstimatedCost"]);
            }

            lblTotalOrdersCount.Text = count.ToString("N0");
            lblTotalOrdersAmount.Text = totalAmt.ToString("N2") + " ج";
        }

        private void OpenNewOrder()
        {
            var frm = new FrmPurchaseOrder(0);
            if (this.ParentForm is FrmMain main)
            {
                main.NavigateTo(frm);
            }
            else
            {
                frm.ShowDialog(this);
                LoadOrders();
            }
        }

        private void OpenSelectedOrder()
        {
            if (dgvOrders.CurrentRow == null) return;
            var drv = dgvOrders.CurrentRow.DataBoundItem as DataRowView;
            if (drv == null) return;

            int oid = Convert.ToInt32(drv["OrderID"]);
            var frm = new FrmPurchaseOrder(oid);
            if (this.ParentForm is FrmMain main)
            {
                main.NavigateTo(frm);
            }
            else
            {
                frm.ShowDialog(this);
                LoadOrders();
            }
        }

        private void ConvertSelectedToPurchase()
        {
            if (dgvOrders.CurrentRow == null)
            {
                MessageBox.Show("يرجى تحديد أمر الشراء المطلوب تحويله لفاتورة مشتريات.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var drv = dgvOrders.CurrentRow.DataBoundItem as DataRowView;
            if (drv == null) return;

            int oid = Convert.ToInt32(drv["OrderID"]);
            var order = PurchaseOrderDAL.GetOrder(oid);
            if (order == null || order.Items.Count == 0)
            {
                MessageBox.Show("الطلبية لا تحتوي على أصناف.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var frmPur = new FrmPurchase();
            frmPur.LoadFromPurchaseOrder(order);

            if (this.ParentForm is FrmMain main)
            {
                main.NavigateTo(frmPur);
            }
            else
            {
                frmPur.Show();
            }
        }

        private void SendWhatsAppSelected()
        {
            if (dgvOrders.CurrentRow == null) return;
            var drv = dgvOrders.CurrentRow.DataBoundItem as DataRowView;
            if (drv == null) return;

            int oid = Convert.ToInt32(drv["OrderID"]);
            var order = PurchaseOrderDAL.GetOrder(oid);
            if (order == null) return;

            string phone = order.SupplierPhone.Replace(" ", "").Replace("-", "");
            if (string.IsNullOrEmpty(phone))
            {
                MessageBox.Show("لا يوجد رقم هاتف مسجل لهذا المورد.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (phone.StartsWith("01")) phone = "20" + phone.Substring(1);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"*طلب توريد بضاعة / أمر شراء جديد*");
            sb.AppendLine($"*الجهة:* {AppConfig.CompanyName}");
            sb.AppendLine($"*رقم الطلبية:* {order.OrderCode}");
            sb.AppendLine($"*التاريخ:* {order.OrderDate:yyyy/MM/dd}");
            sb.AppendLine($"*المطلوب من الأستاذ / الشركة:* {order.SupplierName}");
            sb.AppendLine("────────────────────");
            sb.AppendLine("*بيان الأصناف والكميات المطلوبة:*");

            int idx = 1;
            foreach (var itm in order.Items)
            {
                string pricePart = itm.EstimatedUnitPrice > 0 ? $" | السعر: {itm.EstimatedUnitPrice:N2} ج" : "";
                sb.AppendLine($"{idx}. *{itm.ProductName}*");
                sb.AppendLine($"   الكمية: {itm.Quantity:N2} {itm.UnitName}{pricePart}");
                idx++;
            }

            sb.AppendLine("────────────────────");
            sb.AppendLine($"*إجمالي الأصناف:* {order.Items.Count} صنف");
            sb.AppendLine($"*إجمالي الكمية:* {order.Items.Sum(x => x.Quantity):N2}");
            if (order.Items.Any(x => x.EstimatedUnitPrice > 0))
                sb.AppendLine($"*إجمالي التكلفة التقديرية:* {order.Items.Sum(x => x.TotalPrice):N2} ج");

            if (!string.IsNullOrWhiteSpace(order.Notes))
                sb.AppendLine($"*ملاحظات خاصة:* {order.Notes}");

            sb.AppendLine("\nيرجى مراجعة الطلبية وتأكيد موعد التوريد. شاكرين حسن التعاون معكم.");

            WhatsAppSender.ShowWhatsAppSendOptionsDialog(
                this,
                phone,
                sb.ToString(),
                () => ReceiptImageGenerator.GeneratePurchaseOrderImage(order),
                "إرسال أمر الشراء عبر الواتساب"
            );
        }

        private void PrintSelected()
        {
            if (dgvOrders.CurrentRow == null) return;
            var drv = dgvOrders.CurrentRow.DataBoundItem as DataRowView;
            if (drv == null) return;

            int oid = Convert.ToInt32(drv["OrderID"]);
            var frm = new FrmPurchaseOrder(oid);
            // فتح أمر الشراء
            if (this.ParentForm is FrmMain main)
            {
                main.NavigateTo(frm);
            }
            else
            {
                frm.ShowDialog(this);
            }
        }

        private void CancelSelectedOrder()
        {
            if (dgvOrders.CurrentRow == null) return;
            var drv = dgvOrders.CurrentRow.DataBoundItem as DataRowView;
            if (drv == null) return;

            int oid = Convert.ToInt32(drv["OrderID"]);
            string code = drv["OrderCode"]?.ToString() ?? "";

            var confirm = MessageBox.Show($"هل أنت متأكد من إلغاء أمر الشراء رقم [{code}]؟", "تأكيد الإلغاء", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            if (PurchaseOrderDAL.CancelOrder(oid))
            {
                MessageBox.Show("تم إلغاء الطلبية بنجاح.", "تم", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadOrders();
            }
        }

        private void DeleteSelectedOrder()
        {
            if (dgvOrders.CurrentRow == null) return;
            var drv = dgvOrders.CurrentRow.DataBoundItem as DataRowView;
            if (drv == null) return;

            int oid = Convert.ToInt32(drv["OrderID"]);
            string code = drv["OrderCode"]?.ToString() ?? "";

            var confirm = MessageBox.Show($"هل أنت متأكد من حذف أمر الشراء رقم [{code}] نهائياً من قاعدة البيانات؟", "تأكيد الحذف النهائي", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes) return;

            if (PurchaseOrderDAL.DeleteOrder(oid))
            {
                MessageBox.Show("تم حذف أمر الشراء بنجاح.", "تم الحذف", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadOrders();
            }
        }

        private void ExportToExcel()
        {
            if (dgvOrders.Rows.Count == 0) return;
            using (var dlg = new SaveFileDialog())
            {
                dlg.Title = "تصدير سجل أوامر الشراء إلى Excel";
                dlg.FileName = $"PurchaseOrdersArchive_{DateTime.Now:yyyyMMdd_HHmm}.xls";
                dlg.Filter = "Excel Files (*.xls)|*.xls|All Files (*.*)|*.*";
                dlg.DefaultExt = "xls";

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        FrmReports.ExportDataGridViewToXls(dgvOrders, dlg.FileName, "سجل طلبيات وأوامر الشراء", AppConfig.CompanyName);
                        var res = MessageBox.Show("تم تصدير السجل إلى Excel بنجاح!\nهل تريد فتح الملف الآن؟", "تم التصدير", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                        if (res == DialogResult.Yes)
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dlg.FileName) { UseShellExecute = true });
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("فشل التصدير: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private Panel MakeFilterItem(string label, Control ctrl)
        {
            var pnl = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                RightToLeft = RightToLeft.Yes,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.Transparent,
                Margin = new Padding(4, 2, 4, 2),
                Padding = new Padding(0),
                WrapContents = false
            };

            var lbl = new Label
            {
                Text = label,
                AutoSize = true,
                ForeColor = Theme.TextMain,
                Font = Theme.FontBold,
                Margin = new Padding(2, 6, 2, 0)
            };

            ctrl.Margin = new Padding(1, 2, 3, 2);
            pnl.Controls.Add(lbl);
            pnl.Controls.Add(ctrl);
            return pnl;
        }
    }
}

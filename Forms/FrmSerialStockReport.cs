using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>
    /// تقرير وجرد كميات وسيريلات الأجهزة (IMEI) وتتبع حركتها الشاملة
    /// يتيح عرضاً تفصيلياً لكل سيريال/جهاز، وعرضاً مجمعاً بالكميات والأصناف، والطباعة والتصدير لإكسيل
    /// </summary>
    public class FrmSerialStockReport : Form
    {
        private ComboBox cboWarehouse;
        private ComboBox cboStatus;
        private ComboBox cboViewMode;
        private ComboBox cboProduct;
        private TextBox txtSearch;
        private DateTimePicker dtpFrom;
        private DateTimePicker dtpTo;
        private CheckBox chkUseDateFilter;

        private Button btnLoad;
        private Button btnPrint;
        private Button btnExportExcel;
        private Button btnCopyIMEIs;

        private Label lblTotalAvailableUnits;
        private Label lblTotalAvailableCost;
        private Label lblTotalRetailValue;
        private Label lblTotalExpectedProfit;
        private Label lblTotalSoldUnits;
        private Label lblTotalRealizedProfit;

        private Panel cardCost;
        private Panel cardProfit;
        private Panel cardRealizedProfit;

        private DataGridView dgGrid;
        private DataTable _dtData;
        private bool _canViewCost = true;
        private int _printRowIndex = 0;

        public FrmSerialStockReport()
        {
            _canViewCost = Session.CanViewCost("Inventory") || Session.CanViewCost("Reports") || Session.IsAdmin;
            InitUI();
            LoadWarehouses();
            LoadProducts();
            LoadReport();
        }

        private void InitUI()
        {
            Text = "📱 تقرير وجرد كميات الأجهزة بالسيريال (IMEI) والتتبع الشامل";
            Size = new Size(1300, 780);
            StartPosition = FormStartPosition.CenterScreen;
            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = true;
            BackColor = Theme.BgMain;
            Font = Theme.FontMain;

            // ── 1. اللوحة العلوية: الفلاتر والبحث والتحكم ────────────────────────────
            var pnlTopContainer = new Panel
            {
                Dock = DockStyle.Top,
                Height = 105,
                BackColor = Theme.BgCard,
                Padding = new Padding(8, 4, 8, 4)
            };

            // الصف الأول من الفلاتر
            var pnlRow1 = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 46,
                BackColor = Color.Transparent,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false
            };

            // فلتر المخزن
            pnlRow1.Controls.Add(new Label
            {
                Text = "المخزن:",
                AutoSize = true,
                ForeColor = Theme.TextMain,
                Font = Theme.FontBold,
                Margin = new Padding(4, 12, 2, 0)
            });
            cboWarehouse = new ComboBox
            {
                Width = 140,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(2, 8, 10, 0),
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                FlatStyle = FlatStyle.Flat
            };
            cboWarehouse.SelectedIndexChanged += (s, e) => LoadReport();
            pnlRow1.Controls.Add(cboWarehouse);

            // فلتر حالة السيريال
            pnlRow1.Controls.Add(new Label
            {
                Text = "الحالة:",
                AutoSize = true,
                ForeColor = Theme.TextMain,
                Font = Theme.FontBold,
                Margin = new Padding(4, 12, 2, 0)
            });
            cboStatus = new ComboBox
            {
                Width = 135,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(2, 8, 10, 0),
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                FlatStyle = FlatStyle.Flat
            };
            cboStatus.Items.AddRange(new object[] { "🟢 متاح بالمخزن (In Stock)", "🔴 مباع للعميل (Sold)", "⚪ الكل (جميع السيريلات)" });
            cboStatus.SelectedIndex = 0; // افتراضي: متاح بالمخزن
            cboStatus.SelectedIndexChanged += (s, e) => LoadReport();
            pnlRow1.Controls.Add(cboStatus);

            // نمط العرض
            pnlRow1.Controls.Add(new Label
            {
                Text = "طريقة العرض:",
                AutoSize = true,
                ForeColor = Theme.TextMain,
                Font = Theme.FontBold,
                Margin = new Padding(4, 12, 2, 0)
            });
            cboViewMode = new ComboBox
            {
                Width = 160,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(2, 8, 10, 0),
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                FlatStyle = FlatStyle.Flat
            };
            cboViewMode.Items.AddRange(new object[] { "📋 تفصيلي بالسيريل (جهاز بجهاز)", "📊 ملخص كميات الأصناف (تجميعي)" });
            cboViewMode.SelectedIndex = 0;
            cboViewMode.SelectedIndexChanged += (s, e) => LoadReport();
            pnlRow1.Controls.Add(cboViewMode);

            // فلتر الصنف المحدد
            pnlRow1.Controls.Add(new Label
            {
                Text = "الصنف:",
                AutoSize = true,
                ForeColor = Theme.TextMain,
                Font = Theme.FontBold,
                Margin = new Padding(4, 12, 2, 0)
            });
            cboProduct = new ComboBox
            {
                Width = 170,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(2, 8, 10, 0),
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                FlatStyle = FlatStyle.Flat
            };
            cboProduct.SelectedIndexChanged += (s, e) => LoadReport();
            pnlRow1.Controls.Add(cboProduct);

            // البحث الفوري
            pnlRow1.Controls.Add(new Label
            {
                Text = "🔍 بحث سريع:",
                AutoSize = true,
                ForeColor = Theme.TextMain,
                Font = Theme.FontBold,
                Margin = new Padding(4, 12, 2, 0)
            });
            txtSearch = new TextBox
            {
                Width = 150,
                Margin = new Padding(2, 8, 10, 0),
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain
            };
            txtSearch.TextChanged += (s, e) => LoadReport();
            pnlRow1.Controls.Add(txtSearch);

            // الصف الثاني: التواريخ وأزرار الإجراءات
            var pnlRow2 = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 48,
                BackColor = Color.Transparent,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false
            };

            chkUseDateFilter = new CheckBox
            {
                Text = "📅 تفعيل نطاق التاريخ",
                AutoSize = true,
                ForeColor = Theme.TextMain,
                Font = Theme.FontBold,
                Margin = new Padding(4, 12, 6, 0),
                Checked = false
            };
            chkUseDateFilter.CheckedChanged += (s, e) =>
            {
                dtpFrom.Enabled = chkUseDateFilter.Checked;
                dtpTo.Enabled = chkUseDateFilter.Checked;
                LoadReport();
            };
            pnlRow2.Controls.Add(chkUseDateFilter);

            pnlRow2.Controls.Add(new Label
            {
                Text = "من:",
                AutoSize = true,
                ForeColor = Theme.TextMain,
                Font = Theme.FontBold,
                Margin = new Padding(4, 12, 2, 0)
            });
            dtpFrom = new DateTimePicker
            {
                Width = 115,
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today.AddDays(-30),
                Margin = new Padding(2, 8, 6, 0),
                Enabled = false
            };
            dtpFrom.ValueChanged += (s, e) => { if (chkUseDateFilter.Checked) LoadReport(); };
            pnlRow2.Controls.Add(dtpFrom);

            pnlRow2.Controls.Add(new Label
            {
                Text = "إلى:",
                AutoSize = true,
                ForeColor = Theme.TextMain,
                Font = Theme.FontBold,
                Margin = new Padding(4, 12, 2, 0)
            });
            dtpTo = new DateTimePicker
            {
                Width = 115,
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today,
                Margin = new Padding(2, 8, 12, 0),
                Enabled = false
            };
            dtpTo.ValueChanged += (s, e) => { if (chkUseDateFilter.Checked) LoadReport(); };
            pnlRow2.Controls.Add(dtpTo);

            // أزرار العمليات
            btnLoad = Theme.MakeButton("🔄 تحديث", Theme.Primary);
            btnLoad.Size = new Size(95, 32);
            btnLoad.Margin = new Padding(4, 6, 4, 0);
            btnLoad.Click += (s, e) => LoadReport();
            pnlRow2.Controls.Add(btnLoad);

            btnPrint = Theme.MakeButton("🖨️ طباعة A4", Color.FromArgb(194, 65, 12));
            btnPrint.Size = new Size(110, 32);
            btnPrint.Margin = new Padding(4, 6, 4, 0);
            btnPrint.Click += BtnPrint_Click;
            pnlRow2.Controls.Add(btnPrint);

            btnExportExcel = Theme.MakeButton("📊 إكسيل", Color.FromArgb(16, 185, 129));
            btnExportExcel.Size = new Size(95, 32);
            btnExportExcel.Margin = new Padding(4, 6, 4, 0);
            btnExportExcel.Click += BtnExportExcel_Click;
            pnlRow2.Controls.Add(btnExportExcel);

            btnCopyIMEIs = Theme.MakeButton("📋 نسخ السيريلات", Color.FromArgb(79, 70, 229));
            btnCopyIMEIs.Size = new Size(130, 32);
            btnCopyIMEIs.Margin = new Padding(4, 6, 4, 0);
            btnCopyIMEIs.Click += BtnCopyIMEIs_Click;
            pnlRow2.Controls.Add(btnCopyIMEIs);

            pnlTopContainer.Controls.Add(pnlRow2);
            pnlTopContainer.Controls.Add(pnlRow1);

            // ── 2. لوحة المؤشرات والكروت الإحصائية (KPI Cards) ──────────────────────
            var pnlCards = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 82,
                ColumnCount = 6,
                RowCount = 1,
                Padding = new Padding(8, 4, 8, 4),
                BackColor = Theme.BgMain
            };
            for (int i = 0; i < 6; i++)
            {
                pnlCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / 6f));
            }

            lblTotalAvailableUnits = new Label();
            var card1 = CreateMetricCard("📱 متاح بالمخزن", lblTotalAvailableUnits, "جهاز جاهز للبيع", Color.FromArgb(13, 148, 136), Color.FromArgb(236, 253, 245));

            lblTotalAvailableCost = new Label();
            cardCost = CreateMetricCard("💰 تكلفة المتاح", lblTotalAvailableCost, "إجمالي رأس المال", Color.FromArgb(180, 83, 9), Color.FromArgb(254, 252, 232));

            lblTotalRetailValue = new Label();
            var card3 = CreateMetricCard("🏷️ القيمة البيعية", lblTotalRetailValue, "بسعر البيع المقترح", Color.FromArgb(37, 99, 235), Color.FromArgb(239, 246, 255));

            lblTotalExpectedProfit = new Label();
            cardProfit = CreateMetricCard("📈 أرباح متوقعة", lblTotalExpectedProfit, "هامش ربح المخزون", Color.FromArgb(22, 163, 74), Color.FromArgb(240, 253, 244));

            lblTotalSoldUnits = new Label();
            var card5 = CreateMetricCard("🛒 أجهزة مباعة", lblTotalSoldUnits, "تم تسليمها للعملاء", Color.FromArgb(220, 38, 38), Color.FromArgb(254, 242, 242));

            lblTotalRealizedProfit = new Label();
            cardRealizedProfit = CreateMetricCard("💵 أرباح محققة", lblTotalRealizedProfit, "صافي ربح البيع", Color.FromArgb(109, 40, 217), Color.FromArgb(245, 243, 255));

            cardCost.Visible = _canViewCost;
            cardProfit.Visible = _canViewCost;
            cardRealizedProfit.Visible = _canViewCost;

            pnlCards.Controls.Add(card1, 0, 0);
            pnlCards.Controls.Add(cardCost, 1, 0);
            pnlCards.Controls.Add(card3, 2, 0);
            pnlCards.Controls.Add(cardProfit, 3, 0);
            pnlCards.Controls.Add(card5, 4, 0);
            pnlCards.Controls.Add(cardRealizedProfit, 5, 0);

            // ── 3. جدول البيانات الرئيسي (DataGridView) ──────────────────────────────
            dgGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                RowTemplate = { Height = 28 }
            };
            Theme.ApplyGridTheme(dgGrid);
            dgGrid.CellDoubleClick += DgGrid_CellDoubleClick;

            Controls.Add(dgGrid);
            Controls.Add(pnlCards);
            Controls.Add(pnlTopContainer);
        }

        private Panel CreateMetricCard(string title, Label lblValue, string subtitle, Color primaryColor, Color bgColor)
        {
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = bgColor,
                Padding = new Padding(6),
                Margin = new Padding(3)
            };
            card.Paint += (s, e) =>
            {
                using (var pen = new Pen(primaryColor, 1.5f))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
                }
            };

            var lblT = new Label
            {
                Text = title,
                Dock = DockStyle.Top,
                Height = 18,
                Font = new Font("Segoe UI", 8.8f, FontStyle.Bold),
                ForeColor = primaryColor,
                TextAlign = ContentAlignment.MiddleRight
            };

            lblValue.Dock = DockStyle.Fill;
            lblValue.Text = "0";
            lblValue.Font = new Font("Segoe UI", 12.5f, FontStyle.Bold);
            lblValue.ForeColor = Color.FromArgb(15, 23, 42);
            lblValue.TextAlign = ContentAlignment.MiddleCenter;

            var lblSub = new Label
            {
                Text = subtitle,
                Dock = DockStyle.Bottom,
                Height = 16,
                Font = new Font("Segoe UI", 7.8f, FontStyle.Regular),
                ForeColor = Color.FromArgb(100, 116, 139),
                TextAlign = ContentAlignment.MiddleCenter
            };

            card.Controls.Add(lblValue);
            card.Controls.Add(lblSub);
            card.Controls.Add(lblT);
            return card;
        }

        private void LoadWarehouses()
        {
            cboWarehouse.Items.Clear();
            cboWarehouse.Items.Add(new ComboItem(0, "--- كل المخازن ---"));
            try
            {
                var dt = DbHelper.Query("SELECT WarehouseID, WarehouseName FROM Warehouses WHERE IsActive = 1 ORDER BY WarehouseName");
                foreach (DataRow r in dt.Rows)
                {
                    cboWarehouse.Items.Add(new ComboItem(Convert.ToInt32(r["WarehouseID"]), r["WarehouseName"].ToString()));
                }
            }
            catch { }
            cboWarehouse.SelectedIndex = 0;
        }

        private void LoadProducts()
        {
            cboProduct.Items.Clear();
            cboProduct.Items.Add(new ComboItem(0, "--- جميع الأصناف والأجهزة ---"));
            try
            {
                var dt = DbHelper.Query(@"
                    SELECT DISTINCT p.ProductID, p.ProductName 
                    FROM Products p
                    WHERE p.IsActive = 1 
                      AND (
                          EXISTS (SELECT 1 FROM PurchaseItems pi WHERE pi.ProductID = p.ProductID AND pi.IMEI IS NOT NULL AND LTRIM(RTRIM(pi.IMEI)) <> '')
                          OR EXISTS (SELECT 1 FROM SaleItems si WHERE si.ProductID = p.ProductID AND si.IMEI IS NOT NULL AND LTRIM(RTRIM(si.IMEI)) <> '')
                      )
                    ORDER BY p.ProductName");
                foreach (DataRow r in dt.Rows)
                {
                    cboProduct.Items.Add(new ComboItem(Convert.ToInt32(r["ProductID"]), r["ProductName"].ToString()));
                }
            }
            catch { }
            cboProduct.SelectedIndex = 0;
        }

        private void LoadReport()
        {
            try
            {
                int? wid = null;
                if (cboWarehouse.SelectedItem is ComboItem ciWh && ciWh.ID > 0)
                    wid = ciWh.ID;

                int? pid = null;
                if (cboProduct.SelectedItem is ComboItem ciProd && ciProd.ID > 0)
                    pid = ciProd.ID;

                string status = "All";
                if (cboStatus.SelectedIndex == 0) status = "InStock";
                else if (cboStatus.SelectedIndex == 1) status = "Sold";

                DateTime? from = chkUseDateFilter.Checked ? dtpFrom.Value.Date : (DateTime?)null;
                DateTime? to = chkUseDateFilter.Checked ? dtpTo.Value.Date : (DateTime?)null;
                string search = txtSearch.Text.Trim();

                bool isDetailed = (cboViewMode.SelectedIndex == 0);

                if (isDetailed)
                {
                    SetupDetailedGrid();
                    _dtData = ReportDAL.GetProductSerialsStockReport(from, to, wid, pid, status, search);
                    PopulateDetailedGrid();
                }
                else
                {
                    SetupSummaryGrid();
                    _dtData = ReportDAL.GetProductSerialsStockSummary(wid, null, search);
                    PopulateSummaryGrid(pid);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("فشل تحميل تقرير كميات وسيريلات الأجهزة:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SetupDetailedGrid()
        {
            dgGrid.Columns.Clear();
            dgGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "IMEI", HeaderText = "رقم السيريال (IMEI)", Width = 150 });
            dgGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductCode", HeaderText = "كود الصنف", Width = 95 });
            dgGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "اسم الجهاز / الصنف", Width = 185 });
            dgGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Brand", HeaderText = "الماركة", Width = 95 });
            dgGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Model", HeaderText = "الموديل/المواصفة", Width = 110 });
            dgGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Color", HeaderText = "اللون", Width = 75 });
            dgGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "WarehouseName", HeaderText = "المخزن", Width = 105 });
            dgGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "الحالة", Width = 95 });
            dgGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "DaysInStock", HeaderText = "العمر بالمخزن", Width = 90 });
            dgGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "PurchaseDate", HeaderText = "تاريخ الشراء", Width = 95 });
            dgGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "PurchaseCode", HeaderText = "فاتورة الشراء", Width = 95 });
            dgGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "SupplierName", HeaderText = "المورد", Width = 135 });
            dgGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "PurchasePrice", HeaderText = "سعر الشراء (التكلفة)", Width = 110, Visible = _canViewCost });
            dgGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "SuggestedSalePrice", HeaderText = "سعر البيع المقترح", Width = 110 });
            dgGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProfitMargin", HeaderText = "الربح", Width = 95, Visible = _canViewCost });
            dgGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "SaleDate", HeaderText = "تاريخ البيع", Width = 95 });
            dgGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "SaleCode", HeaderText = "فاتورة البيع", Width = 95 });
            dgGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ClientName", HeaderText = "العميل المشتري", Width = 135 });
            dgGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "SalePrice", HeaderText = "سعر البيع الفعلي", Width = 110 });

            // تخصيص الخطوط والمحاذاة
            dgGrid.Columns["IMEI"].DefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            dgGrid.Columns["IMEI"].DefaultCellStyle.ForeColor = Color.FromArgb(30, 41, 59);
            dgGrid.Columns["Status"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgGrid.Columns["DaysInStock"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        }

        private void PopulateDetailedGrid()
        {
            dgGrid.Rows.Clear();
            if (_dtData == null) return;

            int availableUnits = 0;
            decimal totalAvailableCost = 0;
            decimal totalRetailValue = 0;
            decimal totalExpectedProfit = 0;
            int soldUnits = 0;
            decimal totalRealizedProfit = 0;

            foreach (DataRow r in _dtData.Rows)
            {
                int isAvail = Convert.ToInt32(r["IsAvailable"]);
                decimal pPrice = Convert.ToDecimal(r["PurchasePrice"]);
                decimal sPrice = Convert.ToDecimal(r["SuggestedSalePrice"]);
                decimal profit = Convert.ToDecimal(r["ProfitMargin"]);

                DateTime? pDate = r["PurchaseDate"] != DBNull.Value ? Convert.ToDateTime(r["PurchaseDate"]) : (DateTime?)null;
                DateTime? sDate = r["SaleDate"] != DBNull.Value ? Convert.ToDateTime(r["SaleDate"]) : (DateTime?)null;

                string statusText = r["Status"]?.ToString() ?? "";
                int days = r["DaysInStock"] != DBNull.Value ? Convert.ToInt32(r["DaysInStock"]) : 0;

                if (isAvail == 1)
                {
                    availableUnits++;
                    totalAvailableCost += pPrice;
                    totalRetailValue += sPrice;
                    totalExpectedProfit += (sPrice - pPrice);
                }
                else
                {
                    soldUnits++;
                    totalRealizedProfit += profit;
                }

                int ri = dgGrid.Rows.Add(
                    r["IMEI"],
                    r["ProductCode"],
                    r["ProductName"],
                    r["Brand"],
                    r["Model"],
                    r["Color"],
                    r["WarehouseName"],
                    statusText,
                    days > 0 ? $"{days} يوم" : "اليوم",
                    pDate.HasValue ? pDate.Value.ToString("yyyy/MM/dd") : "-",
                    r["PurchaseCode"],
                    r["SupplierName"],
                    pPrice.ToString("N2"),
                    sPrice.ToString("N2"),
                    profit.ToString("N2"),
                    sDate.HasValue ? sDate.Value.ToString("yyyy/MM/dd") : "-",
                    r["SaleCode"],
                    r["ClientName"],
                    r["SalePrice"] != DBNull.Value ? Convert.ToDecimal(r["SalePrice"]).ToString("N2") : "-"
                );

                var row = dgGrid.Rows[ri];
                if (isAvail == 1)
                {
                    row.Cells["Status"].Style.ForeColor = Color.FromArgb(5, 150, 105);
                    row.Cells["Status"].Style.BackColor = Color.FromArgb(236, 253, 245);
                    row.Cells["Status"].Style.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
                }
                else
                {
                    row.Cells["Status"].Style.ForeColor = Color.FromArgb(220, 38, 38);
                    row.Cells["Status"].Style.BackColor = Color.FromArgb(254, 242, 242);
                    row.Cells["Status"].Style.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
                }
            }

            UpdateMetricCards(availableUnits, totalAvailableCost, totalRetailValue, totalExpectedProfit, soldUnits, totalRealizedProfit);
        }

        private void SetupSummaryGrid()
        {
            dgGrid.Columns.Clear();
            dgGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductCode", HeaderText = "كود الصنف", Width = 95 });
            dgGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "اسم الجهاز / الصنف", Width = 210 });
            dgGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Brand", HeaderText = "الماركة", Width = 100 });
            dgGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Model", HeaderText = "الموديل", Width = 110 });
            dgGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Color", HeaderText = "اللون", Width = 80 });
            dgGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "WarehouseName", HeaderText = "المخزن", Width = 110 });
            dgGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "AvailableCount", HeaderText = "المتاح (سيريال)", Width = 105 });
            dgGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "SoldCount", HeaderText = "المباع", Width = 80 });
            dgGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalPurchasedCount", HeaderText = "إجمالي الوارد", Width = 95 });
            dgGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "AvgPurchasePrice", HeaderText = "متوسط التكلفة", Width = 110, Visible = _canViewCost });
            dgGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "RetailPrice", HeaderText = "سعر البيع", Width = 110 });
            dgGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalAvailableCost", HeaderText = "إجمالي تكلفة المتاح", Width = 125, Visible = _canViewCost });
            dgGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalAvailableRetail", HeaderText = "إجمالي القيمة البيعية", Width = 125 });
            dgGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ExpectedProfit", HeaderText = "الأرباح المتوقعة", Width = 110, Visible = _canViewCost });
            dgGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "AvailableSerials", HeaderText = "أرقام السيريلات المتاحة (IMEI)", Width = 280 });

            dgGrid.Columns["AvailableCount"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgGrid.Columns["AvailableCount"].DefaultCellStyle.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            dgGrid.Columns["AvailableCount"].DefaultCellStyle.ForeColor = Color.FromArgb(5, 150, 105);

            dgGrid.Columns["SoldCount"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgGrid.Columns["TotalPurchasedCount"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        }

        private void PopulateSummaryGrid(int? filterProductID = null)
        {
            dgGrid.Rows.Clear();
            if (_dtData == null) return;

            int availableUnits = 0;
            decimal totalAvailableCost = 0;
            decimal totalRetailValue = 0;
            decimal totalExpectedProfit = 0;
            int soldUnits = 0;

            foreach (DataRow r in _dtData.Rows)
            {
                int pid = Convert.ToInt32(r["ProductID"]);
                if (filterProductID.HasValue && filterProductID.Value > 0 && pid != filterProductID.Value)
                    continue;

                int avail = Convert.ToInt32(r["AvailableCount"]);
                int sold = Convert.ToInt32(r["SoldCount"]);
                int totalPurchased = Convert.ToInt32(r["TotalPurchasedCount"]);
                decimal avgCost = Convert.ToDecimal(r["AvgPurchasePrice"]);
                decimal retail = Convert.ToDecimal(r["RetailPrice"]);
                decimal costVal = Convert.ToDecimal(r["TotalAvailableCost"]);
                decimal retVal = Convert.ToDecimal(r["TotalAvailableRetail"]);
                decimal expProfit = Convert.ToDecimal(r["ExpectedProfit"]);

                availableUnits += avail;
                totalAvailableCost += costVal;
                totalRetailValue += retVal;
                totalExpectedProfit += expProfit;
                soldUnits += sold;

                int ri = dgGrid.Rows.Add(
                    r["ProductCode"],
                    r["ProductName"],
                    r["Brand"],
                    r["Model"],
                    r["Color"],
                    r["WarehouseName"],
                    avail.ToString(),
                    sold.ToString(),
                    totalPurchased.ToString(),
                    avgCost.ToString("N2"),
                    retail.ToString("N2"),
                    costVal.ToString("N2"),
                    retVal.ToString("N2"),
                    expProfit.ToString("N2"),
                    r["AvailableSerials"]
                );

                dgGrid.Rows[ri].Tag = pid;
            }

            UpdateMetricCards(availableUnits, totalAvailableCost, totalRetailValue, totalExpectedProfit, soldUnits, 0);
        }

        private void UpdateMetricCards(int available, decimal cost, decimal retail, decimal expProfit, int sold, decimal realizedProfit)
        {
            lblTotalAvailableUnits.Text = $"{available:N0}";
            lblTotalAvailableCost.Text = $"{cost:N2} ج";
            lblTotalRetailValue.Text = $"{retail:N2} ج";
            lblTotalExpectedProfit.Text = $"{expProfit:N2} ج";
            lblTotalSoldUnits.Text = $"{sold:N0}";
            lblTotalRealizedProfit.Text = $"{realizedProfit:N2} ج";
        }

        private void DgGrid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= dgGrid.Rows.Count) return;

            // في حالة العرض المجمع: النقر المزدوج على الصنف ينقل تلقائياً للعرض التفصيلي لهذا الصنف
            if (cboViewMode.SelectedIndex == 1)
            {
                var row = dgGrid.Rows[e.RowIndex];
                if (row.Tag is int pid && pid > 0)
                {
                    for (int i = 0; i < cboProduct.Items.Count; i++)
                    {
                        if (cboProduct.Items[i] is ComboItem ci && ci.ID == pid)
                        {
                            cboProduct.SelectedIndex = i;
                            break;
                        }
                    }
                    cboViewMode.SelectedIndex = 0; // تحويل للعرض التفصيلي
                }
            }
            else
            {
                // في العرض التفصيلي: النقر المزدوج ينسخ السيريال
                var cell = dgGrid.Rows[e.RowIndex].Cells["IMEI"];
                string imei = cell?.Value?.ToString();
                if (!string.IsNullOrWhiteSpace(imei))
                {
                    try
                    {
                        Clipboard.SetText(imei.Trim());
                        System.Media.SystemSounds.Asterisk.Play();
                        Theme.ShowMsg($"تم نسخ رقم السيريال بنجاح:\n{imei.Trim()}", "تم النسخ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch { }
                }
            }
        }

        private void BtnCopyIMEIs_Click(object sender, EventArgs e)
        {
            if (dgGrid.Rows.Count == 0)
            {
                Theme.ShowMsg("لا توجد سيريلات لنسخها.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var list = new List<string>();
            if (cboViewMode.SelectedIndex == 0)
            {
                foreach (DataGridViewRow row in dgGrid.Rows)
                {
                    string imei = row.Cells["IMEI"]?.Value?.ToString();
                    if (!string.IsNullOrWhiteSpace(imei)) list.Add(imei.Trim());
                }
            }
            else
            {
                foreach (DataGridViewRow row in dgGrid.Rows)
                {
                    string raw = row.Cells["AvailableSerials"]?.Value?.ToString();
                    if (!string.IsNullOrWhiteSpace(raw))
                    {
                        var parts = raw.Split(new[] { " | ", "," }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var p in parts)
                        {
                            if (!string.IsNullOrWhiteSpace(p)) list.Add(p.Trim());
                        }
                    }
                }
            }

            if (list.Count > 0)
            {
                Clipboard.SetText(string.Join(Environment.NewLine, list));
                System.Media.SystemSounds.Asterisk.Play();
                Theme.ShowMsg($"✅ تم نسخ {list.Count} سيريال بنجاح إلى الحافظة!", "تم النسخ", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnExportExcel_Click(object sender, EventArgs e)
        {
            if (dgGrid.Rows.Count == 0)
            {
                Theme.ShowMsg("لا توجد بيانات لتصديرها.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var dlg = new SaveFileDialog())
            {
                dlg.Title = "تصدير تقرير سيريلات وكميات الأجهزة إلى Excel";
                dlg.Filter = "ملف إكسيل (*.xls)|*.xls";
                dlg.FileName = $"تقرير_سيريلات_الأجهزة_{DateTime.Now:yyyyMMdd_HHmm}.xls";
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string title = cboViewMode.SelectedIndex == 0 ? "تقرير تفصيلي لسيريلات الأجهزة (IMEI)" : "ملخص كميات وسيريلات الأجهزة المجمعة";
                        FrmReports.ExportDataGridViewToXls(dgGrid, dlg.FileName, title, AppConfig.CompanyName);

                        var res = MessageBox.Show("✅ تم تصدير التقرير بنجاح!\nهل تريد فتح الملف الآن؟", "تم التصدير", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                        if (res == DialogResult.Yes)
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dlg.FileName) { UseShellExecute = true });
                        }
                    }
                    catch (Exception ex)
                    {
                        Theme.ShowMsg("فشل تصدير الملف:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void BtnPrint_Click(object sender, EventArgs e)
        {
            if (dgGrid.Rows.Count == 0)
            {
                MessageBox.Show("لا توجد بيانات لطباعتها.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _printRowIndex = 0;
            var pd = new PrintDocument();
            pd.DefaultPageSettings.Landscape = true;
            pd.DefaultPageSettings.Margins = new Margins(20, 20, 25, 25);

            pd.PrintPage += Pd_PrintPage;

            using (var ppd = new PrintPreviewDialog())
            {
                ppd.Document = pd;
                ppd.WindowState = FormWindowState.Maximized;
                ppd.ShowDialog(this);
            }
        }

        private void Pd_PrintPage(object sender, PrintPageEventArgs e)
        {
            var g = e.Graphics;
            var marginBounds = e.MarginBounds;
            int y = marginBounds.Top;

            var fontHeader = new Font("Segoe UI", 14f, FontStyle.Bold);
            var fontSub = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            var fontColHeader = new Font("Segoe UI", 8.8f, FontStyle.Bold);
            var fontCell = new Font("Segoe UI", 8.2f, FontStyle.Regular);

            // 1. ترويسة التقرير
            string compName = AppConfig.CompanyName ?? "تقرير كميات وسيريلات الأجهزة";
            string repTitle = cboViewMode.SelectedIndex == 0 ? "📱 تقرير تفصيلي لكميات وسيريلات الأجهزة بالمخازن (IMEI)" : "📊 ملخص كميات وسيريلات الأجهزة المجمعة";
            string printDate = $"تاريخ الطباعة: {DateTime.Now:yyyy/MM/dd HH:mm}   |   المستخدم: {Session.EmpName}";

            var formatRight = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };
            var formatCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            var formatLeft = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };

            g.DrawString(compName, fontHeader, Brushes.DarkSlateBlue, new RectangleF(marginBounds.Left, y, marginBounds.Width, 26), formatRight);
            y += 26;
            g.DrawString(repTitle, new Font("Segoe UI", 11.5f, FontStyle.Bold), Brushes.Black, new RectangleF(marginBounds.Left, y, marginBounds.Width, 22), formatRight);
            y += 22;

            string filterInfo = $"المخزن: {cboWarehouse.Text}   |   الحالة: {cboStatus.Text}   |   الأجهزة المتاحة: {lblTotalAvailableUnits.Text}";
            if (_canViewCost) filterInfo += $"   |   إجمالي التكلفة: {lblTotalAvailableCost.Text}";
            g.DrawString(filterInfo, fontSub, Brushes.DimGray, new RectangleF(marginBounds.Left, y, marginBounds.Width, 18), formatRight);
            y += 18;

            g.DrawString(printDate, fontSub, Brushes.Gray, new RectangleF(marginBounds.Left, y, marginBounds.Width, 16), formatLeft);
            y += 20;

            g.DrawLine(Pens.Gray, marginBounds.Left, y, marginBounds.Right, y);
            y += 8;

            // 2. رسم عناوين الأعمدة
            var visibleCols = dgGrid.Columns.Cast<DataGridViewColumn>().Where(c => c.Visible).ToList();
            int totalColsWidth = visibleCols.Sum(c => c.Width);
            float scale = (float)marginBounds.Width / Math.Max(totalColsWidth, 1);

            int headerHeight = 26;
            g.FillRectangle(new SolidBrush(Color.FromArgb(240, 243, 246)), new Rectangle(marginBounds.Left, y, marginBounds.Width, headerHeight));
            g.DrawRectangle(Pens.DarkGray, new Rectangle(marginBounds.Left, y, marginBounds.Width, headerHeight));

            float curX = marginBounds.Right;
            foreach (var col in visibleCols)
            {
                float colW = col.Width * scale;
                curX -= colW;
                var rect = new RectangleF(curX, y, colW, headerHeight);
                g.DrawRectangle(Pens.LightGray, rect.X, rect.Y, rect.Width, rect.Height);
                g.DrawString(col.HeaderText, fontColHeader, Brushes.Black, rect, formatCenter);
            }
            y += headerHeight;

            // 3. رسم سطور البيانات
            int rowHeight = 22;
            while (_printRowIndex < dgGrid.Rows.Count)
            {
                if (y + rowHeight > marginBounds.Bottom - 30)
                {
                    e.HasMorePages = true;
                    return;
                }

                var row = dgGrid.Rows[_printRowIndex];
                curX = marginBounds.Right;
                bool isEven = (_printRowIndex % 2 == 0);
                if (!isEven)
                {
                    g.FillRectangle(new SolidBrush(Color.FromArgb(248, 250, 252)), new Rectangle(marginBounds.Left, y, marginBounds.Width, rowHeight));
                }

                foreach (var col in visibleCols)
                {
                    float colW = col.Width * scale;
                    curX -= colW;
                    var rect = new RectangleF(curX, y, colW, rowHeight);
                    g.DrawRectangle(Pens.LightGray, rect.X, rect.Y, rect.Width, rect.Height);

                    string val = row.Cells[col.Index].Value?.ToString() ?? "";
                    g.DrawString(val, fontCell, Brushes.Black, rect, formatCenter);
                }

                y += rowHeight;
                _printRowIndex++;
            }

            e.HasMorePages = false;
        }
    }
}

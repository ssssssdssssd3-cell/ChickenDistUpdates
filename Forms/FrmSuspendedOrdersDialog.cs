using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;

namespace ChickenDist.Forms
{
    /// <summary>
    /// نافذة استرجاع ومتابعة أوامر التصنيع المعلقة (تحت التحضير)
    /// تتيح تصفية وبحث وفتح أوامر التصنيع الثابتة والمخصصة بكل سهولة
    /// </summary>
    public class FrmSuspendedOrdersDialog : Form
    {
        public int SelectedProductionID { get; private set; } = 0;
        public string SelectedProductionType { get; private set; } = "";

        private ComboBox cboTypeFilter;
        private TextBox txtSearch;
        private Label lblCountBadge;
        private DataGridView dgOrders;
        private Button btnOpen;
        private Button btnCancelOrder;
        private Button btnClose;

        private readonly string _defaultFilterType;

        public FrmSuspendedOrdersDialog(string defaultFilterType = null)
        {
            _defaultFilterType = defaultFilterType;
            InitUI();
            LoadData();
        }

        private void InitUI()
        {
            this.Text = "⏳ استرجاع ومتابعة أوامر التصنيع المعلقة (تحت التحضير)";
            this.Size = new Size(1140, 600);
            this.MinimumSize = new Size(960, 480);
            this.StartPosition = FormStartPosition.CenterParent;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = false;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            // ── Top Filter Bar ──
            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 56,
                BackColor = Theme.BgCard,
                Padding = new Padding(12, 10, 12, 10)
            };
            this.Controls.Add(pnlTop);

            var flowTop = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                BackColor = Color.Transparent
            };
            pnlTop.Controls.Add(flowTop);

            flowTop.Controls.Add(new Label
            {
                Text = "🔍 تصفية الأوامر المعلقة:",
                AutoSize = true,
                Font = Theme.FontBold,
                ForeColor = Theme.Accent,
                Margin = new Padding(0, 8, 8, 0)
            });

            flowTop.Controls.Add(new Label { Text = "نوع التصنيع:", AutoSize = true, Margin = new Padding(4, 8, 2, 0), Font = Theme.FontSmall });

            cboTypeFilter = new ComboBox
            {
                Width = 160,
                Height = 28,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = Theme.FontMain,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Margin = new Padding(0, 4, 10, 0)
            };
            cboTypeFilter.Items.AddRange(new object[] { "كل الأوامر المعلقة", "تصنيع معياري (ثابت - BOM)", "تصنيع مخصص (مباشر)" });
            
            if (_defaultFilterType == "Fixed") cboTypeFilter.SelectedIndex = 1;
            else if (_defaultFilterType == "Custom") cboTypeFilter.SelectedIndex = 2;
            else cboTypeFilter.SelectedIndex = 0;

            cboTypeFilter.SelectedIndexChanged += (s, e) => LoadData();
            flowTop.Controls.Add(cboTypeFilter);

            flowTop.Controls.Add(new Label { Text = "بحث (كود/اسم/أمر):", AutoSize = true, Margin = new Padding(4, 8, 2, 0), Font = Theme.FontSmall });

            txtSearch = new TextBox
            {
                Width = 220,
                Height = 28,
                Font = Theme.FontMain,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Margin = new Padding(0, 4, 10, 0)
            };
            txtSearch.TextChanged += (s, e) => LoadData();
            flowTop.Controls.Add(txtSearch);

            lblCountBadge = new Label
            {
                Text = "الأوامر المعلقة: 0",
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(234, 88, 12),
                BackColor = Color.FromArgb(255, 247, 237),
                Padding = new Padding(10, 5, 10, 5),
                Margin = new Padding(8, 4, 0, 0)
            };
            flowTop.Controls.Add(lblCountBadge);

            // ── Grid ──
            dgOrders = new DataGridView
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
                RightToLeft = RightToLeft.Yes,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowTemplate = { Height = 34 },
                GridColor = Color.FromArgb(226, 232, 240),
                EnableHeadersVisualStyles = false
            };
            dgOrders.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleCenter
            };
            dgOrders.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White,
                ForeColor = Theme.TextMain,
                Font = Theme.FontMain,
                SelectionBackColor = Color.FromArgb(254, 243, 199),
                SelectionForeColor = Color.FromArgb(15, 23, 42),
                Alignment = DataGridViewContentAlignment.MiddleCenter
            };
            dgOrders.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(248, 250, 252)
            };
            Theme.EnableDoubleBuffer(dgOrders);

            dgOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductionID", Visible = false });
            dgOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductionType", Visible = false });
            dgOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "OrderCode", HeaderText = "كود الأمر", FillWeight = 14 });
            dgOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductionTypeName", HeaderText = "النوع", FillWeight = 14 });
            dgOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "CreatedDate", HeaderText = "تاريخ الإنشاء", FillWeight = 13, DefaultCellStyle = { Format = "yyyy-MM-dd HH:mm" } });
            dgOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductCode", HeaderText = "كود الصنف", FillWeight = 11 });
            dgOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "المنتج النهائي المصنع", FillWeight = 26, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) } });
            dgOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProducedQty", HeaderText = "الكمية", FillWeight = 9, DefaultCellStyle = { Format = "N2", Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Color.FromArgb(2, 132, 199) } });
            dgOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "UnitName", HeaderText = "الوحدة", FillWeight = 8 });
            dgOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "ItemsCount", HeaderText = "الخامات", FillWeight = 9 });
            dgOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalCost", HeaderText = "إجمالي التكلفة", FillWeight = 14, DefaultCellStyle = { Format = "N2", Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Color.FromArgb(217, 119, 6) } });
            dgOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "UnitCost", HeaderText = "تكلفة القطعة", FillWeight = 13, DefaultCellStyle = { Format = "N2", Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Color.FromArgb(16, 185, 129) } });
            dgOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "WarehouseName", HeaderText = "المخزن", FillWeight = 12 });
            dgOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "CreatedByName", HeaderText = "المسؤول", FillWeight = 12 });

            dgOrders.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) OpenSelectedOrder(); };

            // ── Bottom Action Bar ──
            var pnlBottom = new Panel
            {
                Height = 56,
                BackColor = Theme.BgCard,
                Padding = new Padding(12, 8, 12, 8)
            };

            btnOpen = Theme.MakeButton("📂 استرجاع وفتح الأمر المختار", 12, 8, 220, 38, Color.FromArgb(16, 185, 129));
            btnOpen.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            btnOpen.Click += (s, e) => OpenSelectedOrder();
            pnlBottom.Controls.Add(btnOpen);

            btnCancelOrder = Theme.MakeButton("❌ إلغاء الأمر وإرجاع الخامات للمخزن", 240, 8, 240, 38, Color.FromArgb(220, 53, 69));
            btnCancelOrder.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnCancelOrder.Click += (s, e) => CancelSelectedOrder();
            pnlBottom.Controls.Add(btnCancelOrder);

            btnClose = Theme.MakeButton("إغلاق", 490, 8, 100, 38, Color.FromArgb(100, 116, 139));
            btnClose.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
            pnlBottom.Controls.Add(btnClose);

            // ── Main Layout (TableLayoutPanel) لضمان ثبات الواجهة وظهور الترويسات ──
            var tblMain = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                RightToLeft = RightToLeft.Yes,
                BackColor = Theme.BgMain,
                Padding = new Padding(0)
            };
            tblMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 56f)); // Row 0: شريط التصفية والبحث
            tblMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));  // Row 1: جدول الأوامر المعلقة
            tblMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 56f));  // Row 2: أزرار العمليات السفلية

            pnlTop.Dock = DockStyle.Fill;
            pnlTop.Margin = new Padding(0, 0, 0, 4);
            dgOrders.Dock = DockStyle.Fill;
            dgOrders.Margin = new Padding(0, 0, 0, 4);
            pnlBottom.Dock = DockStyle.Fill;
            pnlBottom.Margin = new Padding(0);

            tblMain.Controls.Add(pnlTop, 0, 0);
            tblMain.Controls.Add(dgOrders, 0, 1);
            tblMain.Controls.Add(pnlBottom, 0, 2);

            this.Controls.Clear();
            this.Controls.Add(tblMain);
        }

        private void LoadData()
        {
            try
            {
                string pType = cboTypeFilter.SelectedIndex switch
                {
                    1 => "Fixed",
                    2 => "Custom",
                    _ => null
                };

                var dt = ProductionDAL.GetSuspendedOrders(pType);
                dgOrders.Rows.Clear();

                if (dt != null)
                {
                    string search = txtSearch.Text.Trim().ToLower();

                    foreach (DataRow r in dt.Rows)
                    {
                        string code = r["OrderCode"]?.ToString() ?? "";
                        string pCode = r["ProductCode"]?.ToString() ?? "";
                        string pName = r["ProductName"]?.ToString() ?? "";
                        string whName = r["WarehouseName"]?.ToString() ?? "";
                        string uName = r["CreatedByName"]?.ToString() ?? "";

                        if (!string.IsNullOrEmpty(search))
                        {
                            if (!code.ToLower().Contains(search) &&
                                !pCode.ToLower().Contains(search) &&
                                !pName.ToLower().Contains(search) &&
                                !whName.ToLower().Contains(search) &&
                                !uName.ToLower().Contains(search))
                            {
                                continue;
                            }
                        }

                        dgOrders.Rows.Add(
                            r["ProductionID"],
                            r["ProductionType"],
                            code,
                            r["ProductionTypeName"],
                            r["CreatedDate"],
                            pCode,
                            pName,
                            Convert.ToDecimal(r["ProducedQty"] ?? 0),
                            r["UnitName"]?.ToString() ?? "قطعة",
                            $"{r["ItemsCount"]} صنف",
                            Convert.ToDecimal(r["TotalCost"] ?? 0),
                            Convert.ToDecimal(r["UnitCost"] ?? 0),
                            whName,
                            uName
                        );
                    }

                    lblCountBadge.Text = $"الأوامر المعلقة: {dgOrders.Rows.Count}";
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("FrmSuspendedOrdersDialog.LoadData", ex);
            }
        }

        private void OpenSelectedOrder()
        {
            if (dgOrders.CurrentRow == null || dgOrders.CurrentRow.Cells["ProductionID"].Value == null)
            {
                MessageBox.Show("يرجى اختيار أمر تصنيع معلق من الجدول أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            SelectedProductionID = Convert.ToInt32(dgOrders.CurrentRow.Cells["ProductionID"].Value);
            SelectedProductionType = dgOrders.CurrentRow.Cells["ProductionType"].Value?.ToString() ?? "Fixed";

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void CancelSelectedOrder()
        {
            if (dgOrders.CurrentRow == null || dgOrders.CurrentRow.Cells["ProductionID"].Value == null)
            {
                MessageBox.Show("يرجى اختيار أمر تصنيع معلق للإلغاء.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int pid = Convert.ToInt32(dgOrders.CurrentRow.Cells["ProductionID"].Value);
            string code = dgOrders.CurrentRow.Cells["OrderCode"].Value?.ToString();

            var res = MessageBox.Show(
                $"هل أنت متأكد من رغبتك في إلغاء أمر التصنيع [{code}]؟\nسيتم إرجاع كافة المواد المستهلكة إلى المخزن فوراً.",
                "تأكيد الإلغاء", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (res == DialogResult.Yes)
            {
                if (ProductionDAL.CancelProductionOrder(pid, Session.EmpName, "إلغاء بواسطة المستخدم من نافذة الأوامر المعلقة"))
                {
                    MessageBox.Show("تم إلغاء أمر التصنيع واسترجاع المواد للمخزن بنجاح.", "تم الإلغاء", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadData();
                }
            }
        }
    }
}

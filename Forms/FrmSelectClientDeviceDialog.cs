using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;

namespace ChickenDist.Forms
{
    public class FrmSelectClientDeviceDialog : Form
    {
        private DataTable _dtDevices;
        private DataView _dvDevices;
        private TextBox txtSearch;
        private DataGridView dgvDevices;
        private Button btnSelect;
        private Button btnGeneralSearch;
        private Button btnCancel;

        public DataRow SelectedRow { get; private set; }
        public bool SwitchedToCatalogSearch { get; private set; }

        public FrmSelectClientDeviceDialog(DataTable dtDevices, string clientName, string clientPhone)
        {
            _dtDevices = dtDevices ?? new DataTable();
            _dvDevices = _dtDevices.DefaultView;
            InitUI(clientName, clientPhone);
        }

        private void InitUI(string clientName, string clientPhone)
        {
            this.Text = "اختيار جهاز / صنف من مشتريات العميل";
            this.Size = new Size(920, 520);
            this.MinimumSize = new Size(780, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = false;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;
            this.KeyPreview = true;

            this.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && dgvDevices.Focused)
                {
                    SelectCurrentRow();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.F1)
                {
                    btnGeneralSearch.PerformClick();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    this.DialogResult = DialogResult.Cancel;
                    this.Close();
                    e.Handled = true;
                }
            };

            // 1. Top Header Bar (56px)
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 56,
                BackColor = Color.FromArgb(24, 43, 73),
                Padding = new Padding(15, 6, 15, 6)
            };

            Label lblTitle = new Label
            {
                Text = "قائمة الأجهزة والمشتريات المسجلة للعميل",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                Location = new Point(15, 6),
                AutoSize = true
            };

            string clientInfo = !string.IsNullOrWhiteSpace(clientName) ? "العميل: " + clientName : "";
            if (!string.IsNullOrWhiteSpace(clientPhone)) clientInfo += "  |  الهاتف: " + clientPhone;
            if (string.IsNullOrWhiteSpace(clientInfo)) clientInfo = "اختر الجهاز المطلوب إدخاله تحت الضمان أو ابحث في الدليل العام للأصناف";

            Label lblClientInfo = new Label
            {
                Text = clientInfo,
                ForeColor = Color.FromArgb(200, 220, 255),
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                Location = new Point(15, 30),
                AutoSize = true
            };

            pnlHeader.Controls.AddRange(new Control[] { lblTitle, lblClientInfo });

            // 2. Search & Filter Bar (42px)
            Panel pnlSearch = new Panel
            {
                Dock = DockStyle.Top,
                Height = 42,
                BackColor = Theme.BgCard,
                Padding = new Padding(15, 7, 15, 7),
                BorderStyle = BorderStyle.FixedSingle
            };

            Label lblSearch = new Label
            {
                Text = "تصفية في المشتريات:",
                Dock = DockStyle.Right,
                Width = 145,
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Theme.TextMain,
                Font = Theme.FontBold
            };

            txtSearch = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = Theme.FontMain,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain
            };
            txtSearch.TextChanged += (s, e) => FilterDevices();

            pnlSearch.Controls.Add(txtSearch);
            pnlSearch.Controls.Add(lblSearch);

            // 3. Bottom Action Bar (54px)
            Panel pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 54,
                BackColor = Theme.BgCard,
                BorderStyle = BorderStyle.FixedSingle
            };

            FlowLayoutPanel pnlButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(15, 8, 15, 8),
                WrapContents = false,
                BackColor = Color.Transparent
            };

            btnSelect = Theme.MakeButton("اختيار الجهاز المحدد (Enter)", Theme.Success);
            btnSelect.Size = new Size(220, 36);
            btnSelect.Margin = new Padding(10, 0, 0, 0);
            btnSelect.Click += (s, e) => SelectCurrentRow();

            btnGeneralSearch = Theme.MakeButton("بحث في دليل كل الأصناف (F1)", Color.FromArgb(41, 128, 185));
            btnGeneralSearch.Size = new Size(230, 36);
            btnGeneralSearch.Margin = new Padding(10, 0, 0, 0);
            btnGeneralSearch.Click += (s, e) =>
            {
                SwitchedToCatalogSearch = true;
                this.DialogResult = DialogResult.Retry;
                this.Close();
            };

            btnCancel = Theme.MakeButton("إلغاء (Esc)", Color.FromArgb(100, 110, 120));
            btnCancel.Size = new Size(110, 36);
            btnCancel.Margin = new Padding(10, 0, 0, 0);
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            pnlButtons.Controls.AddRange(new Control[] { btnSelect, btnGeneralSearch, btnCancel });
            pnlFooter.Controls.Add(pnlButtons);

            // 4. Center DataGridView
            dgvDevices = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                RowHeadersVisible = false,
                AutoGenerateColumns = false,
                BackgroundColor = Theme.BgMain,
                BorderStyle = BorderStyle.None
            };
            Theme.ApplyGridTheme(dgvDevices);

            SetupGridColumns();
            dgvDevices.DataSource = _dvDevices;
            dgvDevices.DoubleClick += (s, e) => SelectCurrentRow();

            // Assemble Form
            this.Controls.Add(dgvDevices);
            this.Controls.Add(pnlSearch);
            this.Controls.Add(pnlFooter);
            this.Controls.Add(pnlHeader);
            dgvDevices.BringToFront();
        }

        private void SetupGridColumns()
        {
            dgvDevices.Columns.Clear();

            var colDate = new DataGridViewTextBoxColumn
            {
                DataPropertyName = "SaleDate",
                HeaderText = "تاريخ الفاتورة",
                Width = 100,
                DefaultCellStyle = { Format = "yyyy/MM/dd", Alignment = DataGridViewContentAlignment.MiddleCenter }
            };

            var colCode = new DataGridViewTextBoxColumn
            {
                DataPropertyName = "SaleCode",
                HeaderText = "رقم الفاتورة",
                Width = 110,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Consolas", 9.5f, FontStyle.Bold) }
            };

            var colName = new DataGridViewTextBoxColumn
            {
                DataPropertyName = "ProductName",
                HeaderText = "اسم الجهاز / الصنف",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft }
            };

            var colBrand = new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Brand",
                HeaderText = "الماركة",
                Width = 110,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            };

            var colSerial = new DataGridViewTextBoxColumn
            {
                DataPropertyName = "IMEI",
                HeaderText = "السيريال / IMEI",
                Width = 150,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Consolas", 9.5f, FontStyle.Bold), ForeColor = Color.FromArgb(20, 60, 160) }
            };

            var colQty = new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Quantity",
                HeaderText = "الكمية",
                Width = 65,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            };

            var colPrice = new DataGridViewTextBoxColumn
            {
                DataPropertyName = "UnitPrice",
                HeaderText = "سعر البيع",
                Width = 85,
                DefaultCellStyle = { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleCenter }
            };

            dgvDevices.Columns.AddRange(colDate, colCode, colName, colBrand, colSerial, colQty, colPrice);
        }

        private void FilterDevices()
        {
            if (_dvDevices == null) return;
            string q = txtSearch.Text.Trim().Replace("'", "''");
            if (string.IsNullOrEmpty(q))
            {
                _dvDevices.RowFilter = "";
            }
            else
            {
                _dvDevices.RowFilter = string.Format(
                    "ProductName LIKE '%{0}%' OR Brand LIKE '%{0}%' OR IMEI LIKE '%{0}%' OR SaleCode LIKE '%{0}%'",
                    q);
            }
        }

        private void SelectCurrentRow()
        {
            if (dgvDevices.SelectedRows.Count > 0)
            {
                var drv = dgvDevices.SelectedRows[0].DataBoundItem as DataRowView;
                if (drv != null)
                {
                    SelectedRow = drv.Row;
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            }
            else
            {
                MessageBox.Show("يرجى اختيار جهاز من القائمة أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}

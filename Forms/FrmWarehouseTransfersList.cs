using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>شاشة سجل التحويلات المخزنية السابقة</summary>
    public class FrmWarehouseTransfersList : Form
    {
        private DataGridView dgTransfers, dgTransferItems;
        private DateTimePicker dtpFrom, dtpTo;
        private Button btnLoad;
        private Label lblTransferInfo;

        public FrmWarehouseTransfersList()
        {
            InitUI();
            LoadTransfers();
        }

        private void InitUI()
        {
            this.Text = "سجل التحويلات المخزنية";
            this.Size = new Size(1100, 680);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            // ── Filter Bar ─────────────────────────────────────────────────
            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 55,
                BackColor = Theme.BgCard,
                Padding = new Padding(10)
            };

            pnlTop.Controls.Add(new Label { Text = "من:", Location = new Point(1010, 18), AutoSize = true, ForeColor = Theme.TextMain });
            dtpFrom = new DateTimePicker { Location = new Point(870, 14), Width = 130, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(-30) };

            pnlTop.Controls.Add(new Label { Text = "إلى:", Location = new Point(840, 18), AutoSize = true, ForeColor = Theme.TextMain });
            dtpTo = new DateTimePicker { Location = new Point(700, 14), Width = 130, Format = DateTimePickerFormat.Short, Value = DateTime.Today };

            btnLoad = Theme.MakeButton("🔍 عرض التحويلات", Color.FromArgb(17, 94, 89));
            btnLoad.Location = new Point(540, 11);
            btnLoad.Size = new Size(150, 32);
            btnLoad.Click += (s, e) => LoadTransfers();

            var btnNewTransfer = Theme.MakeButton("🔄 تحويل جديد", Theme.Accent);
            btnNewTransfer.Location = new Point(10, 11);
            btnNewTransfer.Size = new Size(140, 32);
            btnNewTransfer.Click += (s, e) =>
            {
                if (this.ParentForm is FrmMain main)
                    main.NavigateTo(new FrmWarehouseTransfer());
                else
                    new FrmWarehouseTransfer().ShowDialog();
            };

            pnlTop.Controls.AddRange(new Control[] { dtpFrom, dtpTo, btnLoad, btnNewTransfer });

            // ── Split Layout: Transfers list left, Items right ─────────────
            var splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 300,
                BackColor = Theme.BgMain,
                Panel1MinSize = 200,
                Panel2MinSize = 150
            };

            // ── Upper: Transfers list ──────────────────────────────────────
            var pnlUpperLabel = new Label
            {
                Text = "📋 قائمة التحويلات المخزنية",
                Dock = DockStyle.Top,
                Height = 30,
                Font = Theme.FontBold,
                ForeColor = Theme.Accent,
                TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(5, 0, 10, 0),
                BackColor = Theme.BgCard
            };
            splitContainer.Panel1.Controls.Add(pnlUpperLabel);

            dgTransfers = MakeGrid();
            dgTransfers.Columns.Add(new DataGridViewTextBoxColumn { Name = "TransferID", Visible = false });
            dgTransfers.Columns.Add(new DataGridViewTextBoxColumn { Name = "TransferCode", HeaderText = "رقم التحويل", FillWeight = 50 });
            dgTransfers.Columns.Add(new DataGridViewTextBoxColumn { Name = "TransferDate", HeaderText = "التاريخ", FillWeight = 55 });
            dgTransfers.Columns.Add(new DataGridViewTextBoxColumn { Name = "FromWarehouse", HeaderText = "من المستودع", FillWeight = 80 });
            dgTransfers.Columns.Add(new DataGridViewTextBoxColumn { Name = "ToWarehouse", HeaderText = "إلى المستودع", FillWeight = 80 });
            dgTransfers.Columns.Add(new DataGridViewTextBoxColumn { Name = "Notes", HeaderText = "ملاحظات", FillWeight = 100 });
            dgTransfers.Columns.Add(new DataGridViewTextBoxColumn { Name = "CreatedBy", HeaderText = "بواسطة", FillWeight = 60 });
            dgTransfers.SelectionChanged += DgTransfers_SelectionChanged;
            splitContainer.Panel1.Controls.Add(dgTransfers);

            // ── Lower: Transfer items ──────────────────────────────────────
            var pnlLowerLabel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 30,
                BackColor = Theme.BgCard
            };
            lblTransferInfo = new Label
            {
                Text = "🔍 اختر تحويلاً من القائمة لعرض أصنافه",
                Dock = DockStyle.Fill,
                Font = Theme.FontBold,
                ForeColor = Theme.Accent,
                TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(5, 0, 10, 0)
            };
            pnlLowerLabel.Controls.Add(lblTransferInfo);
            splitContainer.Panel2.Controls.Add(pnlLowerLabel);

            dgTransferItems = MakeGrid();
            dgTransferItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "الصنف", FillWeight = 150 });
            dgTransferItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Quantity", HeaderText = "الكمية المحولة", FillWeight = 60 });
            dgTransferItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Unit", HeaderText = "الوحدة", FillWeight = 40 });
            splitContainer.Panel2.Controls.Add(dgTransferItems);

            // ── Assemble ───────────────────────────────────────────────────
            this.Controls.Add(splitContainer);
            this.Controls.Add(pnlTop);

            Theme.ApplyFormRTL(this);
        }

        private void LoadTransfers()
        {
            dgTransfers.Rows.Clear();
            dgTransferItems.Rows.Clear();
            lblTransferInfo.Text = "🔍 اختر تحويلاً من القائمة لعرض أصنافه";

            var dt = TransferDAL.GetAll(dtpFrom.Value, dtpTo.Value);
            foreach (DataRow r in dt.Rows)
            {
                dgTransfers.Rows.Add(
                    r["TransferID"],
                    r["TransferCode"],
                    Convert.ToDateTime(r["TransferDate"]).ToString("dd/MM/yyyy HH:mm"),
                    r["FromWarehouse"],
                    r["ToWarehouse"],
                    r["Notes"],
                    r["CreatedBy"]
                );
            }
        }

        private void DgTransfers_SelectionChanged(object sender, EventArgs e)
        {
            if (dgTransfers.SelectedRows.Count == 0) return;
            var row = dgTransfers.SelectedRows[0];
            if (row.Cells["TransferID"].Value == null) return;

            int transferID = Convert.ToInt32(row.Cells["TransferID"].Value);
            string code = row.Cells["TransferCode"].Value?.ToString();
            string from = row.Cells["FromWarehouse"].Value?.ToString();
            string to   = row.Cells["ToWarehouse"].Value?.ToString();

            lblTransferInfo.Text = $"📦 أصناف التحويل رقم: {code}  |  من: {from}  ←  إلى: {to}";

            dgTransferItems.Rows.Clear();
            var dt = TransferDAL.GetItems(transferID);
            foreach (DataRow r in dt.Rows)
            {
                dgTransferItems.Rows.Add(
                    r["ProductName"],
                    Convert.ToDecimal(r["Quantity"]).ToString("N3"),
                    r["Unit"]
                );
            }
        }

        private DataGridView MakeGrid()
        {
            return new DataGridView
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
                GridColor = Theme.BorderColor,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Theme.BgCard, ForeColor = Theme.TextMain,
                    SelectionBackColor = Theme.Primary, SelectionForeColor = Color.White,
                    Font = Theme.FontMain
                },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Theme.Primary, ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold)
                },
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
        }
    }
}

using System;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    public class FrmProductMovement : Form
    {
        private int _productID;
        private string _productName;
        private string _productUnit;

        private DataGridView dgMovement;
        private DateTimePicker dtpFrom, dtpTo;
        private Button btnLoad, btnPrint;
        private Label lblTitle;

        public FrmProductMovement(int productID, string productName, string productUnit)
        {
            _productID = productID;
            _productName = productName;
            _productUnit = productUnit;
            InitUI();
            LoadMovement();
        }

        private void InitUI()
        {
            this.Text = $"حركة الصنف - {_productName}";
            this.Size = new Size(950, 650);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            // Header Title
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 55, BackColor = Theme.BgCard, Padding = new Padding(10) };
            lblTitle = new Label 
            { 
                Text = $"📊 تقرير حركة الصنف: {_productName} ({_productUnit})", 
                Font = Theme.FontHeader, 
                ForeColor = Theme.Accent,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlHeader.Controls.Add(lblTitle);
            this.Controls.Add(pnlHeader);

            // Filter Bar
            var pnlFilters = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Theme.BgCard, Padding = new Padding(10) };
            
            var lblFrom = new Label { Text = "من:", AutoSize = true, ForeColor = Theme.TextMain, Location = new Point(880, 15) };
            dtpFrom = new DateTimePicker 
            { 
                Location = new Point(730, 11), 
                Width = 140, 
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today.AddDays(-30)
            };

            var lblTo = new Label { Text = "إلى:", AutoSize = true, ForeColor = Theme.TextMain, Location = new Point(680, 15) };
            dtpTo = new DateTimePicker 
            { 
                Location = new Point(530, 11), 
                Width = 140, 
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today
            };

            btnLoad = Theme.MakeButton("🔍 عرض الحركة", Color.FromArgb(60, 100, 60));
            btnLoad.Location = new Point(390, 8);
            btnLoad.Size = new Size(120, 32);
            btnLoad.Click += (s, e) => LoadMovement();

            btnPrint = Theme.MakeButton("🖨 طباعة تقرير الحركة", Theme.Accent);
            btnPrint.Location = new Point(20, 8);
            btnPrint.Size = new Size(160, 32);
            btnPrint.Click += (s, e) => PrintMovement();

            pnlFilters.Controls.AddRange(new Control[] { lblFrom, dtpFrom, lblTo, dtpTo, btnLoad, btnPrint });
            this.Controls.Add(pnlFilters);

            // Grid
            dgMovement = new DataGridView
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
                DefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.BgCard, ForeColor = Theme.TextMain, SelectionBackColor = Theme.Primary, SelectionForeColor = Color.White, Font = Theme.FontMain },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Theme.Primary, ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold) },
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            dgMovement.Columns.Add(new DataGridViewTextBoxColumn { Name = "TransDate", HeaderText = "التاريخ", FillWeight = 45 });
            dgMovement.Columns.Add(new DataGridViewTextBoxColumn { Name = "TransType", HeaderText = "نوع الحركة", FillWeight = 50 });
            dgMovement.Columns.Add(new DataGridViewTextBoxColumn { Name = "RefCode", HeaderText = "المستند", FillWeight = 40 });
            dgMovement.Columns.Add(new DataGridViewTextBoxColumn { Name = "PersonName", HeaderText = "العميل / المندوب", FillWeight = 60 });
            dgMovement.Columns.Add(new DataGridViewTextBoxColumn { Name = "QtyIn", HeaderText = "وارد (+)", FillWeight = 30 });
            dgMovement.Columns.Add(new DataGridViewTextBoxColumn { Name = "QtyOut", HeaderText = "صادر (-)", FillWeight = 30 });
            dgMovement.Columns.Add(new DataGridViewTextBoxColumn { Name = "Balance", HeaderText = "الرصيد الحالي", FillWeight = 35 });
            dgMovement.Columns.Add(new DataGridViewTextBoxColumn { Name = "Notes", HeaderText = "البيان", FillWeight = 70 });

            this.Controls.Add(dgMovement);

            // Apply responsive Z-Order docking:
            pnlHeader.BringToFront();
            pnlFilters.BringToFront();
            dgMovement.BringToFront();

            Theme.ApplyFormRTL(this);
        }

        private void LoadMovement()
        {
            dgMovement.Rows.Clear();
            DataTable dt = InventoryDAL.GetProductMovement(_productID);
            
            decimal runningBalance = 0;
            decimal openingBalance = 0;
            
            // 1. Calculate opening balance before the selected start date
            foreach (DataRow r in dt.Rows)
            {
                DateTime date = Convert.ToDateTime(r["TransDate"]);
                decimal qtyIn = Convert.ToDecimal(r["QtyIn"]);
                decimal qtyOut = Convert.ToDecimal(r["QtyOut"]);

                if (date.Date < dtpFrom.Value.Date)
                {
                    openingBalance += qtyIn - qtyOut;
                }
            }

            runningBalance = openingBalance;

            // 2. Add Opening Balance row if date filter starts later
            dgMovement.Rows.Add(
                "---",
                "رصيد ما قبل الفترة",
                "---",
                "---",
                "---",
                "---",
                openingBalance.ToString("N2"),
                $"الرصيد الدفتري المتراكم قبل تاريخ {dtpFrom.Value:dd/MM/yyyy}"
            );
            dgMovement.Rows[0].DefaultCellStyle.ForeColor = Theme.TextSub;
            dgMovement.Rows[0].DefaultCellStyle.Font = new Font(Theme.FontMain, FontStyle.Italic);

            // 3. Add movements within the selected period
            foreach (DataRow r in dt.Rows)
            {
                DateTime date = Convert.ToDateTime(r["TransDate"]);
                if (date.Date >= dtpFrom.Value.Date && date.Date <= dtpTo.Value.Date)
                {
                    decimal qtyIn = Convert.ToDecimal(r["QtyIn"]);
                    decimal qtyOut = Convert.ToDecimal(r["QtyOut"]);
                    runningBalance += qtyIn - qtyOut;

                    int rowIndex = dgMovement.Rows.Add(
                        date.ToString("dd/MM/yyyy HH:mm"),
                        r["TransType"],
                        r["RefCode"],
                        r["PersonName"],
                        qtyIn > 0 ? qtyIn.ToString("N2") : "",
                        qtyOut > 0 ? qtyOut.ToString("N2") : "",
                        runningBalance.ToString("N2"),
                        r["Notes"]
                    );

                    // Color code rows for readability
                    if (qtyIn > 0)
                        dgMovement.Rows[rowIndex].Cells["QtyIn"].Style.ForeColor = Color.LightGreen;
                    if (qtyOut > 0)
                        dgMovement.Rows[rowIndex].Cells["QtyOut"].Style.ForeColor = Color.OrangeRed;
                }
            }
        }

        private void PrintMovement()
        {
            var pd = new PrintDocument();
            pd.DefaultPageSettings.Landscape = false;
            pd.DefaultPageSettings.PaperSize = new PaperSize("A4", 827, 1169); // A4 standard portrait
            
            pd.PrintPage += (s, e) =>
            {
                var g = e.Graphics;
                var boldBig = new Font("Arial", 16, FontStyle.Bold);
                var bold = new Font("Arial", 10, FontStyle.Bold);
                var normal = new Font("Arial", 9);
                var small = new Font("Arial", 8);
                var center = new StringFormat { Alignment = StringAlignment.Center };
                var right = new StringFormat { Alignment = StringAlignment.Far };

                int y = 30;
                int pageW = 800; // Margins at 20px left and right

                // Title
                g.DrawString("تقرير كشف حركة صنف", boldBig, Brushes.DarkBlue, new RectangleF(20, y, pageW - 40, 30), center); y += 30;
                g.DrawString("شركة توزيع الكتاكيت", bold, Brushes.Black, new RectangleF(20, y, pageW - 40, 22), center); y += 25;
                g.DrawLine(new Pen(Color.DarkBlue, 2), 20, y, pageW - 20, y); y += 15;

                // Meta Info
                g.DrawString($"الصنف: {_productName} ({_productUnit})", bold, Brushes.Black, 20, y);
                g.DrawString($"الفترة: من {dtpFrom.Value:dd/MM/yyyy} إلى {dtpTo.Value:dd/MM/yyyy}", normal, Brushes.Black, new RectangleF(20, y, pageW - 40, 20), right);
                y += 25;
                g.DrawLine(Pens.Gray, 20, y, pageW - 20, y); y += 10;

                // Table columns: Date, Type, Ref, Party, In, Out, Balance
                int[] xCols = { 20, 130, 260, 350, 480, 560, 640 };
                string[] headers = { "التاريخ", "نوع الحركة", "المستند", "العميل/المندوب", "وارد (+)", "صادر (-)", "الرصيد" };
                
                for (int i = 0; i < headers.Length; i++)
                {
                    g.DrawString(headers[i], bold, Brushes.DarkBlue, xCols[i], y);
                }
                y += 22;
                g.DrawLine(Pens.Gray, 20, y, pageW - 20, y); y += 8;

                // Draw rows
                foreach (DataGridViewRow row in dgMovement.Rows)
                {
                    if (y > 1100) // Simple page overflow safeguard
                    {
                        g.DrawString("يتبع في الصفحة التالية...", small, Brushes.Gray, 20, y);
                        break;
                    }

                    string date = row.Cells["TransDate"].Value?.ToString();
                    string type = row.Cells["TransType"].Value?.ToString();
                    string refCode = row.Cells["RefCode"].Value?.ToString();
                    string party = row.Cells["PersonName"].Value?.ToString();
                    string qtyIn = row.Cells["QtyIn"].Value?.ToString();
                    string qtyOut = row.Cells["QtyOut"].Value?.ToString();
                    string balance = row.Cells["Balance"].Value?.ToString();

                    g.DrawString(date, normal, Brushes.Black, xCols[0], y);
                    g.DrawString(type, normal, Brushes.Black, xCols[1], y);
                    g.DrawString(refCode, normal, Brushes.Black, xCols[2], y);
                    
                    // Truncate party name if too long for spacing
                    if (party.Length > 20) party = party.Substring(0, 18) + "..";
                    g.DrawString(party, normal, Brushes.Black, xCols[3], y);
                    
                    g.DrawString(qtyIn, normal, Brushes.LightGreen, xCols[4], y);
                    g.DrawString(qtyOut, normal, Brushes.OrangeRed, xCols[5], y);
                    g.DrawString(balance, bold, Brushes.Black, xCols[6], y);

                    y += 20;
                }

                y += 10;
                g.DrawLine(new Pen(Color.DarkBlue, 1.5f), 20, y, pageW - 20, y); y += 8;
                g.DrawString($"تاريخ الطباعة: {DateTime.Now:dd/MM/yyyy HH:mm}   |   نظام التوزيع الذكي", small, Brushes.Gray, 20, y);
            };

            var preview = new PrintPreviewDialog
            {
                Document = pd,
                Width = 850,
                Height = 750,
                Text = $"معاينة حركة صنف - {_productName}"
            };
            preview.ShowDialog();
        }
    }
}

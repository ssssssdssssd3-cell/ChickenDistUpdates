using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    /// <summary>شاشة إدارة الأصناف</summary>
    public class FrmProducts : Form
    {
        private DataGridView dgProducts;
        private TextBox txtCode, txtName, txtUnit, txtDescription, txtInternationalBarcode;
        private NumericUpDown nudPrice, nudPurchasePrice, nudMinStockLimit;
        private CheckBox chkActive, chkHasBarcode;
        private Button btnNew, btnSave, btnDelete;
        private int _selectedID = 0;

        public FrmProducts()
        {
            InitUI();
            LoadProducts();
            ClearDetail();
        }

        private void InitUI()
        {
            this.Text = "إدارة الأصناف";
            this.Size = new Size(900, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            // header handled by main form's top bar

            var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical, FixedPanel = FixedPanel.Panel1 };

            // Left: Grid (Panel2 in RTL)
            dgProducts = new DataGridView
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
            dgProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductID", Visible = false });
            dgProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductCode", HeaderText = "الكود", FillWeight = 35 });
            dgProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "اسم الصنف" });
            dgProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "Unit", HeaderText = "الوحدة", FillWeight = 35 });
            dgProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "SalePrice", HeaderText = "السعر", FillWeight = 40 });
            dgProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "IsActive", HeaderText = "نشط", FillWeight = 25 });
            dgProducts.SelectionChanged += DgProducts_SelectionChanged;
            split.Panel2.Controls.Add(dgProducts);

            // Right: Detail (Panel1 in RTL)
            split.Panel1.BackColor = Theme.BgCard;
            split.Panel1.Padding = new Padding(15);
            split.Panel1.AutoScroll = true;

            int y = 20;
            AddField(split.Panel1, "كود الصنف:", ref y, out txtCode);
            txtCode.ReadOnly = true;
            AddField(split.Panel1, "اسم الصنف:", ref y, out txtName);
            AddField(split.Panel1, "الوحدة:", ref y, out txtUnit);

            var lblPurchasePrice = new Label { Text = "سعر الشراء:", Location = new Point(250, y), AutoSize = true, ForeColor = Theme.TextMain };
            split.Panel1.Controls.Add(lblPurchasePrice);
            nudPurchasePrice = new NumericUpDown { Location = new Point(15, y - 2), Width = 180, Minimum = 0, Maximum = 999999, DecimalPlaces = 2, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };
            split.Panel1.Controls.Add(nudPurchasePrice); y += 40;

            if (!Session.CanShowCostProfit("Products"))
            {
                lblPurchasePrice.Visible = false;
                nudPurchasePrice.Visible = false;
            }

            split.Panel1.Controls.Add(new Label { Text = "سعر البيع:", Location = new Point(250, y), AutoSize = true, ForeColor = Theme.TextMain });
            nudPrice = new NumericUpDown { Location = new Point(15, y - 2), Width = 180, Minimum = 0, Maximum = 999999, DecimalPlaces = 2, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };
            split.Panel1.Controls.Add(nudPrice); y += 40;

            split.Panel1.Controls.Add(new Label { Text = "حد الطلب:", Location = new Point(250, y), AutoSize = true, ForeColor = Theme.TextMain });
            nudMinStockLimit = new NumericUpDown { Location = new Point(15, y - 2), Width = 180, Minimum = 0, Maximum = 999999, DecimalPlaces = 3, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };
            split.Panel1.Controls.Add(nudMinStockLimit); y += 40;

            AddField(split.Panel1, "الوصف:", ref y, out txtDescription);
            AddField(split.Panel1, "الكود الدولي:", ref y, out txtInternationalBarcode);

            chkHasBarcode = new CheckBox { Text = "له ملصق باركود مطبوع", Location = new Point(140, y), Width = 180, ForeColor = Theme.TextMain, Checked = true };
            chkActive = new CheckBox { Text = "صنف نشط", Location = new Point(40, y), Width = 90, ForeColor = Theme.TextMain, Checked = true }; y += 40;
            split.Panel1.Controls.AddRange(new Control[] { chkHasBarcode, chkActive });

            btnNew = Theme.MakeButton("🆕 جديد", 240, y, 90, 32, Color.FromArgb(60, 100, 60));
            btnSave = Theme.MakeButton("💾 حفظ", 140, y, 90, 32, Theme.Accent);
            btnDelete = Theme.MakeButton("🗑 إيقاف", 40, y, 90, 32, Color.FromArgb(140, 40, 40)); y += 40;
            
            var btnPrintBarcode = Theme.MakeButton("🖨️ طباعة ملصق باركود", 40, y, 290, 32, Theme.Primary);
            
            btnNew.Click += (s, e) => ClearDetail();
            btnSave.Click += BtnSave_Click;
            btnDelete.Click += BtnDelete_Click;
            btnPrintBarcode.Click += BtnPrintBarcode_Click;
            split.Panel1.Controls.AddRange(new Control[] { btnNew, btnSave, btnDelete, btnPrintBarcode });
            this.Controls.Add(split);
            split.SplitterDistance = 350;

            Theme.ApplyFormRTL(this);
        }

        private void AddField(Control parent, string label, ref int y, out TextBox txt)
        {
            parent.Controls.Add(new Label { Text = label, Location = new Point(250, y), AutoSize = true, ForeColor = Theme.TextMain });
            txt = new TextBox { Location = new Point(15, y - 2), Width = 180, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };
            parent.Controls.Add(txt);
            y += 38;
        }

        private void LoadProducts()
        {
            dgProducts.Rows.Clear();
            var dt = ProductDAL.GetAll();
            foreach (DataRow r in dt.Rows)
            {
                bool active = Convert.ToBoolean(r["IsActive"]);
                var ri = dgProducts.Rows.Add(r["ProductID"], r["ProductCode"], r["ProductName"],
                    r["Unit"], Convert.ToDecimal(r["SalePrice"]).ToString("N2"), active ? "✓" : "✗");
                if (!active) dgProducts.Rows[ri].DefaultCellStyle.ForeColor = Color.Gray;
            }
        }

        private void DgProducts_SelectionChanged(object sender, EventArgs e)
        {
            if (dgProducts.SelectedRows.Count == 0) return;
            _selectedID = Convert.ToInt32(dgProducts.SelectedRows[0].Cells["ProductID"].Value);
            var dr = ProductDAL.GetByID(_selectedID);
            if (dr == null) return;
            txtCode.Text = dr["ProductCode"].ToString();
            txtName.Text = dr["ProductName"].ToString();
            txtUnit.Text = dr["Unit"].ToString();
            nudPurchasePrice.Value = Convert.ToDecimal(dr["PurchasePrice"] == DBNull.Value ? 0 : dr["PurchasePrice"]);
            nudPrice.Value = Convert.ToDecimal(dr["SalePrice"]);
            nudMinStockLimit.Value = Convert.ToDecimal(dr["MinStockLimit"] == DBNull.Value ? 0 : dr["MinStockLimit"]);
            txtDescription.Text = dr["Description"].ToString();
            txtInternationalBarcode.Text = dr["InternationalBarcode"] != DBNull.Value ? dr["InternationalBarcode"].ToString() : "";
            chkHasBarcode.Checked = dr["HasBarcode"] == DBNull.Value || Convert.ToBoolean(dr["HasBarcode"]);
            chkActive.Checked = Convert.ToBoolean(dr["IsActive"]);
        }

        private void ClearDetail()
        {
            _selectedID = 0;
            txtCode.Text = ProductDAL.GetNextProductCode();
            txtName.Clear(); txtUnit.Text = "رأس";
            nudPurchasePrice.Value = 0;
            nudPrice.Value = 0;
            nudMinStockLimit.Value = 0;
            txtDescription.Clear();
            txtInternationalBarcode.Clear();
            chkHasBarcode.Checked = true;
            chkActive.Checked = true;
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text)) { MessageBox.Show("أدخل اسم الصنف"); return; }
            int id = ProductDAL.Save(_selectedID, txtCode.Text, txtName.Text, txtUnit.Text, nudPrice.Value, chkActive.Checked,
                nudPurchasePrice.Value, nudMinStockLimit.Value, txtDescription.Text, txtInternationalBarcode.Text, chkHasBarcode.Checked);
            if (id > 0) { MessageBox.Show("✅ تم الحفظ"); _selectedID = id; LoadProducts(); }
            else MessageBox.Show("❌ فشل الحفظ");
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (_selectedID == 0) return;
            if (MessageBox.Show("إيقاف الصنف؟", "تأكيد", MessageBoxButtons.YesNo) == DialogResult.Yes)
            { ProductDAL.Delete(_selectedID); LoadProducts(); ClearDetail(); }
        }

        private void BtnPrintBarcode_Click(object sender, EventArgs e)
        {
            if (_selectedID == 0)
            {
                MessageBox.Show("اختر صنفاً أولاً لطباعة الباركود الخاص به.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var dr = ProductDAL.GetByID(_selectedID);
            if (dr == null) return;

            bool hasBarcode = dr["HasBarcode"] == DBNull.Value || Convert.ToBoolean(dr["HasBarcode"]);
            if (!hasBarcode)
            {
                MessageBox.Show("هذا الصنف محدد بأنه 'ليس له باركود' في كارت الصنف.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Ask for quantity of prints
            Form prompt = new Form()
            {
                Width = 300,
                Height = 160,
                Text = "طباعة ملصقات الباركود",
                StartPosition = FormStartPosition.CenterParent,
                RightToLeft = RightToLeft.Yes,
                RightToLeftLayout = true,
                BackColor = Theme.BgCard,
                Font = Theme.FontMain,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };
            Label textLabel = new Label() { Left = 20, Top = 20, Text = "عدد الملصقات المطلوب طباعتها:", AutoSize = true, ForeColor = Theme.TextMain };
            NumericUpDown input = new NumericUpDown() { Left = 20, Top = 45, Width = 240, Minimum = 1, Maximum = 100, Value = 1, BackColor = Theme.BgInput, ForeColor = Theme.TextMain };
            Button confirmation = Theme.MakeButton("🖨️ طباعة", 100, 85, 100, 30, Theme.Success);
            confirmation.Click += (senderPrompt, ePrompt) => { prompt.DialogResult = DialogResult.OK; prompt.Close(); };
            prompt.Controls.Add(textLabel);
            prompt.Controls.Add(input);
            prompt.Controls.Add(confirmation);

            // Apply Theme RTL to dialog
            Theme.ApplyRTL(prompt.Controls);

            if (prompt.ShowDialog() == DialogResult.OK)
            {
                int qty = (int)input.Value;
                // Use international barcode if filled, otherwise local product code
                string codeToPrint = dr["InternationalBarcode"] != DBNull.Value && !string.IsNullOrWhiteSpace(dr["InternationalBarcode"].ToString())
                    ? dr["InternationalBarcode"].ToString()
                    : dr["ProductCode"].ToString();

                PrintBarcodeLabels(codeToPrint, dr["ProductName"].ToString(), Convert.ToDecimal(dr["SalePrice"]), qty);
            }
        }

        private void PrintBarcodeLabels(string code, string name, decimal price, int qty)
        {
            try
            {
                var pd = new PrintDocument();
                if (AppConfig.ThermalPrinterEnabled && !string.IsNullOrEmpty(AppConfig.ThermalPrinterName))
                {
                    pd.PrinterSettings.PrinterName = AppConfig.ThermalPrinterName;
                }
                
                pd.DefaultPageSettings.PaperSize = new PaperSize("BarcodeLabel", 180, 100);
                pd.DefaultPageSettings.Margins = new Margins(5, 5, 5, 5);

                int printedCount = 0;
                pd.PrintPage += (s, ev) =>
                {
                    var g = ev.Graphics;
                    var nameFont = new Font("Arial", 8, FontStyle.Bold);
                    var codeFont = new Font("Arial", 10, FontStyle.Bold);
                    var priceFont = new Font("Arial", 8);

                    int pageW = ev.PageBounds.Width;
                    int pageH = ev.PageBounds.Height;
                    
                    var center = new StringFormat { Alignment = StringAlignment.Center };

                    g.DrawString(AppConfig.CompanyName, priceFont, Brushes.Black, new RectangleF(0, 5, pageW, 14), center);
                    g.DrawString(name, nameFont, Brushes.Black, new RectangleF(0, 20, pageW, 16), center);
                    g.DrawString($"* {code} *", codeFont, Brushes.Black, new RectangleF(0, 40, pageW, 20), center);
                    
                    // Simulated barcode lines
                    int startX = 30;
                    int endX = pageW - 30;
                    int barY = 60;
                    int barH = 15;
                    Pen thinPen = new Pen(Color.Black, 1.5f);
                    Pen thickPen = new Pen(Color.Black, 3f);
                    
                    for (int x = startX; x < endX; x += 4)
                    {
                        if (x % 3 == 0)
                            g.DrawLine(thickPen, x, barY, x, barY + barH);
                        else
                            g.DrawLine(thinPen, x, barY, x, barY + barH);
                    }

                    g.DrawString($"السعر: {price:N2} ج", nameFont, Brushes.Black, new RectangleF(0, 80, pageW, 16), center);

                    printedCount++;
                    ev.HasMorePages = printedCount < qty;
                };

                pd.Print();
                MessageBox.Show($"✅ تم إرسال {qty} ملصق باركود للطابعة بنجاح!", "نجاح الطباعة", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ فشلت طباعة الباركود:\n{ex.Message}", "خطأ طباعة", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }}


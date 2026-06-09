using System;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Windows.Forms;
using System.Collections.Generic;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    public class FrmPrintPurchaseBarcodes : Form
    {
        private int _purchaseID;
        private string _purchaseCode;
        private DataGridView dgItems;
        private ComboBox cboPrinters;
        private CheckBox chkPrintPrice;
        private CheckBox chkPrintCompanyName;
        private Button btnPrint;
        private Button btnCancel;

        // متغيرات تتبع حالة الطباعة
        private List<BarcodePrintItem> _printList;
        private int _currentItemIndex = 0;
        private int _currentLabelIndex = 0;

        public FrmPrintPurchaseBarcodes(int purchaseID, string purchaseCode)
        {
            _purchaseID = purchaseID;
            _purchaseCode = purchaseCode;

            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            this.Text = $"🏷️ طباعة باركود أصناف الفاتورة: {_purchaseCode}";
            this.Size = new Size(750, 560);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;

            // شريط العنوان
            var pnlTop = Theme.MakeTitleBar("🏷️ طباعة ملصقات الباركود", $"تحديد كميات الطباعة للملصقات (الافتراضي هو الكميات المشتراة) للفاتورة {_purchaseCode}");
            this.Controls.Add(pnlTop);

            // الجدول
            dgItems = new DataGridView
            {
                Location = new Point(20, 80),
                Size = new Size(700, 300),
                BackgroundColor = Theme.BgCard,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RightToLeft = RightToLeft.Yes,
                GridColor = Theme.BorderColor,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Theme.BgCard,
                    ForeColor = Theme.TextMain,
                    SelectionBackColor = Theme.Primary,
                    SelectionForeColor = Color.White,
                    Font = Theme.FontMain
                },
                ColumnHeadersHeight = 35,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Theme.Primary,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    Alignment = DataGridViewContentAlignment.MiddleCenter
                },
                EnableHeadersVisualStyles = false
            };

            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductID", Visible = false });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "الصنف", ReadOnly = true, FillWeight = 160 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductCode", HeaderText = "الباركود", ReadOnly = true, FillWeight = 90 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "Price", HeaderText = "سعر البيع", ReadOnly = true, FillWeight = 70 });
            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "PurchasedQty", HeaderText = "الكمية المشتراة", ReadOnly = true, FillWeight = 80 });
            
            var colPrintQty = new DataGridViewTextBoxColumn
            {
                Name = "PrintQty",
                HeaderText = "عدد الملصقات",
                FillWeight = 80
            };
            colPrintQty.DefaultCellStyle.BackColor = Theme.BgInput;
            colPrintQty.DefaultCellStyle.ForeColor = Theme.Accent;
            colPrintQty.DefaultCellStyle.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            colPrintQty.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgItems.Columns.Add(colPrintQty);

            this.Controls.Add(dgItems);

            // لوحة الإعدادات والطباعة بالأسفل
            int y = 395;

            var lblPrinter = new Label
            {
                Text = "اختر طابعة الملصقات:",
                Location = new Point(20, y + 4),
                AutoSize = true,
                ForeColor = Theme.TextMain
            };
            this.Controls.Add(lblPrinter);

            cboPrinters = new ComboBox
            {
                Location = new Point(160, y),
                Width = 240,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain
            };
            try
            {
                cboPrinters.Items.Add("(طابعة الباركود الافتراضية)");
                foreach (string printer in PrinterSettings.InstalledPrinters)
                {
                    cboPrinters.Items.Add(printer);
                }
                cboPrinters.SelectedItem = string.IsNullOrEmpty(AppConfig.ReceiptPrinterName) ? "(طابعة الباركود الافتراضية)" : AppConfig.ReceiptPrinterName;
                if (cboPrinters.SelectedIndex == -1 && cboPrinters.Items.Count > 0)
                    cboPrinters.SelectedIndex = 0;
            }
            catch { }
            this.Controls.Add(cboPrinters);

            chkPrintPrice = new CheckBox
            {
                Text = "طباعة السعر على الملصق",
                Location = new Point(420, y),
                AutoSize = true,
                ForeColor = Theme.TextMain,
                Checked = true
            };
            this.Controls.Add(chkPrintPrice);

            chkPrintCompanyName = new CheckBox
            {
                Text = "طباعة اسم المؤسسة",
                Location = new Point(420, y + 25),
                AutoSize = true,
                ForeColor = Theme.TextMain,
                Checked = true
            };
            this.Controls.Add(chkPrintCompanyName);

            y += 65;

            // أزرار
            btnPrint = Theme.MakeButton("🖨️ طباعة الملصقات", 20, y, 160, 38, Theme.Success);
            btnPrint.Click += BtnPrint_Click;
            this.Controls.Add(btnPrint);

            btnCancel = Theme.MakeButton("إلغاء ↩", 195, y, 110, 38, Color.FromArgb(70, 80, 95));
            btnCancel.Click += (s, e) => this.Close();
            this.Controls.Add(btnCancel);
        }

        private void LoadData()
        {
            try
            {
                var dt = DbHelper.Query(@"
                    SELECT pi.ProductID, pr.ProductCode, pr.ProductName, pi.Quantity,
                           COALESCE(pi.SuggestedSalePrice, pr.SalePrice, 0) AS Price
                    FROM PurchaseItems pi
                    JOIN Products pr ON pi.ProductID = pr.ProductID
                    WHERE pi.PurchaseID = @id", DbHelper.P("@id", _purchaseID));

                dgItems.Rows.Clear();
                foreach (DataRow r in dt.Rows)
                {
                    decimal qty = Convert.ToDecimal(r["Quantity"]);
                    // جعل عدد الملصقات الافتراضي مساوياً للكمية المشتراة (أو 1 كحد أدنى لو كانت الكميات كسرية)
                    int printQty = (int)Math.Max(1, Math.Floor(qty));

                    dgItems.Rows.Add(
                        r["ProductID"],
                        r["ProductName"],
                        r["ProductCode"],
                        qty.ToString("N2"),
                        r["Price"].ToString(),
                        printQty
                    );
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("فشل تحميل بيانات الأصناف:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnPrint_Click(object sender, EventArgs e)
        {
            _printList = new List<BarcodePrintItem>();

            foreach (DataGridViewRow row in dgItems.Rows)
            {
                if (row.Cells["PrintQty"].Value == null) continue;
                int.TryParse(row.Cells["PrintQty"].Value.ToString(), out int pQty);

                if (pQty > 0)
                {
                    _printList.Add(new BarcodePrintItem
                    {
                        ProductName = row.Cells["ProductName"].Value.ToString(),
                        ProductCode = row.Cells["ProductCode"].Value.ToString(),
                        Price       = Convert.ToDecimal(row.Cells["Price"].Value),
                        PrintQty    = pQty
                    });
                }
            }

            if (_printList.Count == 0)
            {
                MessageBox.Show("يرجى تحديد عدد ملصقات أكبر من الصفر لصنف واحد على الأقل.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _currentItemIndex = 0;
            _currentLabelIndex = 0;

            try
            {
                var pd = new PrintDocument();
                // حجم ورق الاستيكر القياسي بالـ 1/100 بوصة (مثال: 50x30 مم يعادل تقريباً 2.0x1.2 بوصة)
                pd.DefaultPageSettings.PaperSize = new PaperSize("StickerLabel", 200, 120);
                pd.DefaultPageSettings.Margins = new Margins(5, 5, 5, 5);

                if (cboPrinters.SelectedIndex > 0)
                {
                    pd.PrinterSettings.PrinterName = cboPrinters.SelectedItem.ToString();
                }
                else if (!string.IsNullOrEmpty(AppConfig.ReceiptPrinterName))
                {
                    AppConfig.SetPrinter(pd, AppConfig.ReceiptPrinterName);
                }

                pd.PrintPage += Pd_PrintPage;

                var prev = new PrintPreviewDialog
                {
                    Document = pd,
                    Width = 450,
                    Height = 400,
                    Text = "معاينة ملصقات الباركود"
                };
                prev.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show("فشل بدء عملية الطباعة:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Pd_PrintPage(object sender, PrintPageEventArgs e)
        {
            if (_currentItemIndex >= _printList.Count)
            {
                e.HasMorePages = false;
                return;
            }

            var item = _printList[_currentItemIndex];
            var g = e.Graphics;

            // الخطوط والمسافات للتصميم الفيكتور الصغير
            var fCompany = new Font("Arial", 6f, FontStyle.Regular);
            var fName    = new Font("Arial", 7.5f, FontStyle.Bold);
            var fPrice   = new Font("Arial", 8.5f, FontStyle.Bold);
            var fCode    = new Font("Courier New", 7.5f, FontStyle.Regular);

            int w = e.PageBounds.Width;   // 200 units (2 inches)
            int h = e.PageBounds.Height;  // 120 units (1.2 inches)
            float y = 4;

            var center = new StringFormat { Alignment = StringAlignment.Center };

            // 1. طباعة اسم الشركة
            if (chkPrintCompanyName.Checked)
            {
                g.DrawString(AppConfig.CompanyName, fCompany, Brushes.Black, new RectangleF(0, y, w, 10), center);
                y += 10;
            }

            // 2. طباعة اسم الصنف (ملتف إذا كان طويلاً)
            g.DrawString(item.ProductName, fName, Brushes.Black, new RectangleF(2, y, w - 4, 24), center);
            y += 24;

            // 3. رسم الباركود (Code 39)
            float barcodeHeight = 36;
            DrawCode39(g, item.ProductCode, 10, y, w - 20, barcodeHeight);
            y += barcodeHeight + 2;

            // 4. طباعة النص للباركود
            g.DrawString(item.ProductCode, fCode, Brushes.Black, new RectangleF(0, y, w, 12), center);
            y += 10;

            // 5. طباعة السعر
            if (chkPrintPrice.Checked)
            {
                g.DrawString($"السعر: {item.Price:N2} ج", fPrice, Brushes.DarkRed, new RectangleF(0, y, w, 14), center);
            }

            // تتبع الكمية المتبقية من الاستيكر الحالي
            _currentLabelIndex++;
            if (_currentLabelIndex >= item.PrintQty)
            {
                _currentItemIndex++;
                _currentLabelIndex = 0;
            }

            // تحديد إذا كان هناك صفحات أخرى
            e.HasMorePages = (_currentItemIndex < _printList.Count);
        }

        // رسم باركود Code 39 القياسي بألوان GDI+ بدون خطوط خارجية
        private static void DrawCode39(Graphics g, string code, float x, float y, float width, float height)
        {
            try
            {
                string textToEncode = "*" + code.ToUpper().Trim() + "*";
                
                // خريطة ترميز Code 39 (1 = خط عريض/فراغ عريض، 0 = خط رفيع/فراغ رفيع)
                // يتكون كل حرف من 9 عناصر: 5 خطوط و4 فراغات (3 عريضة و6 رفيعة)
                var map = new Dictionary<char, string>
                {
                    {'0', "000110100"}, {'1', "100010001"}, {'2', "001010001"}, {'3', "101000001"},
                    {'4', "000010101"}, {'5', "100000101"}, {'6', "001000101"}, {'7', "000000111"},
                    {'8', "100000110"}, {'9', "001000110"}, {'A', "100010010"}, {'B', "001010010"},
                    {'C', "101010000"}, {'D', "000010011"}, {'E', "100000011"}, {'F', "001000011"},
                    {'G', "000000110"}, {'H', "100000110"}, {'I', "001000110"}, {'J', "000000110"},
                    {'K', "100000001"}, {'L', "001000001"}, {'M', "101000000"}, {'N', "000010001"},
                    {'O', "100010000"}, {'P', "001010000"}, {'Q', "000000011"}, {'R', "100000011"},
                    {'S', "001000011"}, {'T', "000010010"}, {'U', "110000001"}, {'V', "011000001"},
                    {'W', "111000000"}, {'X', "010010001"}, {'Y', "110010000"}, {'Z', "011010000"},
                    {'-', "010000101"}, {'.', "110000100"}, {' ', "011000100"}, {'*', "010010100"}
                };

                // حساب معامل العرض
                // كل حرف به: 6 عناصر رفيعة و3 عناصر عريضة + 1 فراغ رفيع فاصل بين الحروف
                // إجمالي الوحدات لكل حرف: 6 * 1 (رفيع) + 3 * 3 (عريض) + 1 * 1 (فاصل) = 16 وحدة.
                float totalUnits = textToEncode.Length * 16;
                float moduleWidth = width / totalUnits;
                if (moduleWidth < 0.4f) moduleWidth = 0.4f;

                float curX = x;
                using (var brush = new SolidBrush(Color.Black))
                {
                    foreach (char c in textToEncode)
                    {
                        string pattern;
                        if (!map.TryGetValue(c, out pattern))
                            pattern = map['*']; // افتراضي للنجوم الفاصلة

                        for (int i = 0; i < 9; i++)
                        {
                            bool isBar = (i % 2 == 0);
                            bool isWide = (pattern[i] == '1');
                            float elementWidth = isWide ? (moduleWidth * 3) : moduleWidth;

                            if (isBar)
                            {
                                g.FillRectangle(brush, curX, y, elementWidth, height);
                            }

                            curX += elementWidth;
                        }

                        // الفراغ الفاصل بين الحروف
                        curX += moduleWidth;
                    }
                }
            }
            catch { }
        }
    }

    public class BarcodePrintItem
    {
        public string ProductName { get; set; }
        public string ProductCode { get; set; }
        public decimal Price { get; set; }
        public int PrintQty { get; set; }
    }
}

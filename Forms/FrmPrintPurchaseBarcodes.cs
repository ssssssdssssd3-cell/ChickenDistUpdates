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
        private ComboBox cboBarcodeTemplate;
        private ComboBox cboBarcodeEncoding;
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
            this.Size = new Size(750, 610);
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

            dgItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "ShelfLocation", Visible = false });

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
                cboPrinters.SelectedItem = string.IsNullOrEmpty(AppConfig.BarcodePrinterName) ? "(طابعة الباركود الافتراضية)" : AppConfig.BarcodePrinterName;
                if (cboPrinters.SelectedIndex == -1 && cboPrinters.Items.Count > 0)
                    cboPrinters.SelectedIndex = 0;
            }
            catch { }
            this.Controls.Add(cboPrinters);
            y += 35;

            // Sticker Template
            var lblTemplate = new Label
            {
                Text = "شكل ملصق الباركود:",
                Location = new Point(20, y + 4),
                AutoSize = true,
                ForeColor = Theme.TextMain
            };
            this.Controls.Add(lblTemplate);

            cboBarcodeTemplate = new ComboBox
            {
                Location = new Point(160, y),
                Width = 240,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain
            };
            cboBarcodeTemplate.Items.AddRange(new object[]
            {
                "الافتراضي (اسم صنف + سعر + باركود)",
                "سعر بارز (سعر كبير + باركود)",
                "ملصق صغير (سعر وباركود فقط)",
                "ملصق الرف (اسم صنف وسعر كبير - بدون باركود)"
            });
            cboBarcodeTemplate.SelectedItem = AppConfig.BarcodeTemplate == "PriceHeavy" ? "سعر بارز (سعر كبير + باركود)"
                                            : AppConfig.BarcodeTemplate == "Small" ? "ملصق صغير (سعر وباركود فقط)"
                                            : AppConfig.BarcodeTemplate == "Shelf" ? "ملصق الرف (اسم صنف وسعر كبير - بدون باركود)"
                                            : "الافتراضي (اسم صنف + سعر + باركود)";
            if (cboBarcodeTemplate.SelectedIndex == -1) cboBarcodeTemplate.SelectedIndex = 0;
            this.Controls.Add(cboBarcodeTemplate);
            y += 35;

            // Barcode Encoding
            var lblEncoding = new Label
            {
                Text = "نوع تشفير الباركود:",
                Location = new Point(20, y + 4),
                AutoSize = true,
                ForeColor = Theme.TextMain
            };
            this.Controls.Add(lblEncoding);

            cboBarcodeEncoding = new ComboBox
            {
                Location = new Point(160, y),
                Width = 240,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain
            };
            cboBarcodeEncoding.Items.AddRange(new object[]
            {
                "Code 128 (موصى به - سريع وسهل القراءة)",
                "Code 39 (أحادي عريض)"
            });
            cboBarcodeEncoding.SelectedIndex = AppConfig.BarcodeEncoding == "Code39" ? 1 : 0;
            this.Controls.Add(cboBarcodeEncoding);

            chkPrintPrice = new CheckBox
            {
                Text = "طباعة السعر على الملصق",
                Location = new Point(420, 395),
                AutoSize = true,
                ForeColor = Theme.TextMain,
                Checked = true
            };
            this.Controls.Add(chkPrintPrice);

            chkPrintCompanyName = new CheckBox
            {
                Text = "طباعة اسم المؤسسة",
                Location = new Point(420, 420),
                AutoSize = true,
                ForeColor = Theme.TextMain,
                Checked = true
            };
            this.Controls.Add(chkPrintCompanyName);

            y += 45;

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
                           COALESCE(pi.SuggestedSalePrice, pr.SalePrice, 0) AS Price,
                           pr.ShelfLocation, pr.PrintLocalBarcode
                    FROM PurchaseItems pi
                    JOIN Products pr ON pi.ProductID = pr.ProductID
                    WHERE pi.PurchaseID = @id", DbHelper.P("@id", _purchaseID));

                dgItems.Rows.Clear();
                foreach (DataRow r in dt.Rows)
                {
                    decimal qty = Convert.ToDecimal(r["Quantity"]);
                    
                    bool printLocal = true;
                    if (r.Table.Columns.Contains("PrintLocalBarcode") && r["PrintLocalBarcode"] != DBNull.Value)
                    {
                        printLocal = Convert.ToBoolean(r["PrintLocalBarcode"]);
                    }

                    // جعل عدد الملصقات الافتراضي مساوياً للكمية المشتراة لو الصنف يطبع له باركود محلي، وإلا 0
                    int printQty = printLocal ? (int)Math.Max(1, Math.Floor(qty)) : 0;

                    dgItems.Rows.Add(
                        r["ProductID"],
                        r["ProductName"],
                        r["ProductCode"],
                        Convert.ToDecimal(r["Price"]).ToString("N2"),
                        qty.ToString("N2"),
                        printQty,
                        r["ShelfLocation"]?.ToString() ?? ""
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
                        PrintQty    = pQty,
                        ShelfLocation = row.Cells["ShelfLocation"].Value?.ToString() ?? ""
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
                pd.PrintController = new StandardPrintController();
                
                if (cboPrinters.SelectedIndex > 0)
                {
                    pd.PrinterSettings.PrinterName = cboPrinters.SelectedItem.ToString();
                }
                else if (!string.IsNullOrEmpty(AppConfig.BarcodePrinterName))
                {
                    AppConfig.SetPrinter(pd, AppConfig.BarcodePrinterName);
                }
                else if (!string.IsNullOrEmpty(AppConfig.ReceiptPrinterName))
                {
                    AppConfig.SetPrinter(pd, AppConfig.ReceiptPrinterName);
                }

                AppConfig.SetPaperSize(pd, AppConfig.BarcodeStickerSize);

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

            var g = e.Graphics;

            string template = "Standard";
            bool isCode128 = true;
            
            if (cboBarcodeTemplate.SelectedItem != null)
            {
                template = cboBarcodeTemplate.SelectedIndex == 1 ? "PriceHeavy"
                         : cboBarcodeTemplate.SelectedIndex == 2 ? "Small"
                         : cboBarcodeTemplate.SelectedIndex == 3 ? "Shelf"
                         : "Standard";
            }
            if (cboBarcodeEncoding.SelectedItem != null)
            {
                isCode128 = cboBarcodeEncoding.SelectedIndex == 0;
            }

            string labelType = (AppConfig.BarcodeStickerSize == "38x26_double") ? "Split" : "Full";
            int labelsPerRow = (labelType == "Split") ? 2 : 1;

            float pageWidth = e.PageBounds.Width;
            float pageHeight = e.PageBounds.Height;
            float leftMargin = 5;
            float topMargin = 5;

            float labelWidth = labelType == "Full" ? pageWidth : (pageWidth / labelsPerRow);
            float labelHeight = pageHeight - (topMargin * 2);

            bool isSmallSticker = (AppConfig.BarcodeStickerSize == "38x26" || AppConfig.BarcodeStickerSize == "38x26_double");

            var fCompany  = new Font("Arial", isSmallSticker ? 6.5f : 8f, FontStyle.Bold);
            var fName     = new Font("Arial", isSmallSticker ? 6.5f : 7.5f, FontStyle.Bold);
            var fPrice    = new Font("Arial", isSmallSticker ? 7.5f : 8.5f, FontStyle.Bold);
            var fCode     = new Font("Courier New", isSmallSticker ? 6.5f : 7.5f, FontStyle.Regular);
            var fLocation = new Font("Arial", isSmallSticker ? 6.5f : 7.5f, FontStyle.Bold);

            var fPriceLarge    = new Font("Arial", isSmallSticker ? 11f : 14f, FontStyle.Bold);
            var fNameLarge     = new Font("Arial", isSmallSticker ? 8.5f : 10f, FontStyle.Bold);
            var fLocationLarge = new Font("Arial", isSmallSticker ? 7.5f : 9f, FontStyle.Bold);

            var center = new StringFormat { Alignment = StringAlignment.Center };
            var leftFormat = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };
            var rightFormat = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };

            // Print labels on the current page row
            for (int itemIndex = 0; itemIndex < labelsPerRow; itemIndex++)
            {
                if (_currentItemIndex >= _printList.Count)
                    break;

                var item = _printList[_currentItemIndex];

                int currentColumn = itemIndex % labelsPerRow;
                int currentRow = itemIndex / labelsPerRow;

                float startX = leftMargin + (currentColumn * labelWidth);
                float startY = topMargin + (currentRow * labelHeight);

                // Use local variables bounded to the label
                float x = startX;
                float y = startY;
                float w = labelWidth - (leftMargin * 2);

                if (template == "Shelf")
                {
                    if (chkPrintCompanyName.Checked)
                    {
                        g.DrawString(AppConfig.CompanyName, fCompany, Brushes.Gray, new RectangleF(x, y, w, isSmallSticker ? 10 : 12), center);
                        y += isSmallSticker ? 11 : 14;
                    }
                    g.DrawString(item.ProductName, fNameLarge, Brushes.Black, new RectangleF(x + 2, y, w - 4, isSmallSticker ? 22 : 30), center);
                    y += isSmallSticker ? 24 : 32;
                    if (chkPrintPrice.Checked)
                    {
                        g.DrawString($"{item.Price:N2} ج", fPriceLarge, Brushes.DarkRed, new RectangleF(x, y, w, isSmallSticker ? 18 : 24), center);
                        y += isSmallSticker ? 20 : 26;
                    }
                    if (!string.IsNullOrWhiteSpace(item.ShelfLocation))
                    {
                        g.DrawString($"الرف / مكان الصنف: {item.ShelfLocation}", fLocationLarge, Brushes.Black, new RectangleF(x, y, w, isSmallSticker ? 12 : 16), center);
                    }
                }
                else if (template == "Small")
                {
                    g.DrawString(item.ProductName, new Font("Arial", isSmallSticker ? 6f : 6.5f, FontStyle.Bold), Brushes.Black, new RectangleF(x + 2, y, w - 4, isSmallSticker ? 12 : 16), center);
                    y += isSmallSticker ? 12 : 16;

                    float barcodeHeight = isSmallSticker ? 22 : 32;
                    float barcodeX = x + (w - (w - 20)) / 2;
                    if (isCode128)
                        DrawCode128(g, item.ProductCode, barcodeX, y, w - 20, barcodeHeight);
                    else
                        DrawCode39(g, item.ProductCode, barcodeX, y, w - 20, barcodeHeight);
                    y += barcodeHeight + 2;

                    g.DrawString(item.ProductCode, new Font("Courier New", isSmallSticker ? 6f : 6.5f), Brushes.Black, new RectangleF(x, y, w, isSmallSticker ? 8 : 10), center);
                    y += isSmallSticker ? 8 : 10;

                    if (chkPrintPrice.Checked)
                    {
                        g.DrawString($"{item.Price:N2} ج", fPrice, Brushes.DarkRed, new RectangleF(x, y, w, isSmallSticker ? 10 : 12), center);
                    }
                }
                else if (template == "PriceHeavy")
                {
                    if (chkPrintPrice.Checked)
                    {
                        g.DrawString($"{item.Price:N2} ج", fPriceLarge, Brushes.DarkRed, new RectangleF(x + 5, y, w - 10, isSmallSticker ? 18 : 22), center);
                        y += isSmallSticker ? 20 : 24;
                    }
                    g.DrawString(item.ProductName, fName, Brushes.Black, new RectangleF(x + 2, y, w - 4, isSmallSticker ? 14 : 18), center);
                    y += isSmallSticker ? 14 : 18;

                    float barcodeHeight = isSmallSticker ? 20 : 30;
                    float barcodeX = x + (w - (w - 20)) / 2;
                    if (isCode128)
                        DrawCode128(g, item.ProductCode, barcodeX, y, w - 20, barcodeHeight);
                    else
                        DrawCode39(g, item.ProductCode, barcodeX, y, w - 20, barcodeHeight);
                    y += barcodeHeight + 2;

                    g.DrawString(item.ProductCode, fCode, Brushes.Black, new RectangleF(x + 5, y, w / 2 - 5, isSmallSticker ? 10 : 12), leftFormat);
                    if (!string.IsNullOrWhiteSpace(item.ShelfLocation))
                    {
                        g.DrawString($"الرف: {item.ShelfLocation}", fLocation, Brushes.Black, new RectangleF(x + w / 2, y, w / 2 - 5, isSmallSticker ? 10 : 12), rightFormat);
                    }
                }
                else
                {
                    if (chkPrintCompanyName.Checked)
                    {
                        g.DrawString(AppConfig.CompanyName, fCompany, Brushes.Black, new RectangleF(x, y, w, isSmallSticker ? 10 : 12), center);
                        y += isSmallSticker ? 10 : 12;
                    }
                    g.DrawString(item.ProductName, fName, Brushes.Black, new RectangleF(x + 2, y, w - 4, isSmallSticker ? 16 : 24), center);
                    y += isSmallSticker ? 16 : 24;

                    float barcodeHeight = isSmallSticker ? 24 : 36;
                    float barcodeX = x + (w - (w - 20)) / 2;
                    if (isCode128)
                        DrawCode128(g, item.ProductCode, barcodeX, y, w - 20, barcodeHeight);
                    else
                        DrawCode39(g, item.ProductCode, barcodeX, y, w - 20, barcodeHeight);
                    y += barcodeHeight + 2;

                    g.DrawString(item.ProductCode, fCode, Brushes.Black, new RectangleF(x, y, w, isSmallSticker ? 10 : 12), center);
                    y += isSmallSticker ? 10 : 12;

                    float bottomY = y;
                    if (chkPrintPrice.Checked)
                    {
                        g.DrawString($"السعر: {item.Price:N2} ج", fPrice, Brushes.DarkRed, new RectangleF(x + 5, bottomY, w / 2 - 5, isSmallSticker ? 11 : 14), leftFormat);
                    }
                    if (!string.IsNullOrWhiteSpace(item.ShelfLocation))
                    {
                        g.DrawString($"الرف: {item.ShelfLocation}", fLocation, Brushes.Black, new RectangleF(x + w / 2, bottomY, w / 2 - 5, isSmallSticker ? 11 : 14), rightFormat);
                    }
                }

                _currentLabelIndex++;
                if (_currentLabelIndex >= item.PrintQty)
                {
                    _currentItemIndex++;
                    _currentLabelIndex = 0;
                }
            }

            e.HasMorePages = (_currentItemIndex < _printList.Count);
        }

        private static readonly string[] Code128Patterns = new string[]
        {
            "212222", "222122", "222221", "121223", "121322", "131222", "122213", "122312", "132212", "221213",
            "221312", "231212", "112232", "122132", "122231", "113222", "123122", "123221", "223211", "221132",
            "221231", "213212", "223112", "312131", "311222", "321122", "321221", "312212", "322112", "322211",
            "212123", "212321", "232121", "111323", "131123", "131321", "112313", "132113", "132311", "211313",
            "231113", "231311", "112133", "112331", "132131", "113123", "113321", "133121", "313121", "211331",
            "231131", "213113", "213311", "213131", "311123", "311321", "331121", "312113", "312311", "332111",
            "314111", "221411", "431111", "111224", "111422", "121124", "121421", "141122", "141221", "112214",
            "112412", "122114", "122411", "142112", "142211", "241211", "221114", "413111", "241112", "134111",
            "111242", "121142", "121241", "114212", "124112", "124211", "411212", "421112", "421211", "212141",
            "214121", "412121", "111143", "111341", "131141", "114113", "114311", "411113", "411311", "113141",
            "114131", "311141", "411131", "211412", "211214", "211232", "2331112"
        };

        private static void DrawCode128(Graphics g, string code, float x, float y, float width, float height)
        {
            try
            {
                code = code.Trim();
                if (string.IsNullOrEmpty(code)) return;

                // Set GDI+ options for crisp, aliased rendering (perfect for barcodes)
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.None;

                int sum = 104; // Start B
                var symbolIndices = new List<int>();
                symbolIndices.Add(104); // Start B

                for (int i = 0; i < code.Length; i++)
                {
                    int val = code[i] - 32;
                    if (val < 0 || val > 102) val = 0; // fallback to Space
                    symbolIndices.Add(val);
                    sum += val * (i + 1);
                }

                int checksum = sum % 103;
                symbolIndices.Add(checksum);
                symbolIndices.Add(106); // Stop

                int totalModules = 0;
                foreach (int index in symbolIndices)
                {
                    string pattern = Code128Patterns[index];
                    foreach (char c in pattern)
                    {
                        totalModules += (c - '0');
                    }
                }

                float moduleWidth = width / totalModules;
                
                // Cap module width to prevent extremely fat bleeding bars for short codes
                float maxModuleWidth = (width < 140f) ? 1.5f : 2.0f; 
                if (moduleWidth > maxModuleWidth)
                {
                    moduleWidth = maxModuleWidth;
                }
                if (moduleWidth < 1.0f) moduleWidth = 1.0f;

                // Center the barcode
                float actualBarcodeWidth = totalModules * moduleWidth;
                float curX = x + (width - actualBarcodeWidth) / 2;

                using (var brush = new SolidBrush(Color.Black))
                {
                    foreach (int index in symbolIndices)
                    {
                        string pattern = Code128Patterns[index];
                        for (int i = 0; i < pattern.Length; i++)
                        {
                            bool isBar = (i % 2 == 0);
                            float elementWidth = (pattern[i] - '0') * moduleWidth;
                            float nextX = curX + elementWidth;

                            if (isBar)
                            {
                                int rectX = (int)Math.Round(curX);
                                int rectW = (int)Math.Round(nextX) - rectX;
                                g.FillRectangle(brush, rectX, y, rectW, height);
                            }
                            curX = nextX;
                        }
                    }
                }
            }
            catch { }
        }

        // رسم باركود Code 39 القياسي بألوان GDI+ بدون خطوط خارجية
        private static void DrawCode39(Graphics g, string code, float x, float y, float width, float height)
        {
            try
            {
                string textToEncode = "*" + code.ToUpper().Trim() + "*";
                
                // Set GDI+ options for crisp, aliased rendering (perfect for barcodes)
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.None;

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
                
                // Cap module width to prevent extremely fat bleeding bars for short codes
                float maxModuleWidth = (width < 140f) ? 1.5f : 2.0f; 
                if (moduleWidth > maxModuleWidth)
                {
                    moduleWidth = maxModuleWidth;
                }
                if (moduleWidth < 1.0f) moduleWidth = 1.0f;

                // Center the barcode
                float actualBarcodeWidth = totalUnits * moduleWidth;
                float curX = x + (width - actualBarcodeWidth) / 2;

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
                            float nextX = curX + elementWidth;

                            if (isBar)
                            {
                                int rectX = (int)Math.Round(curX);
                                int rectW = (int)Math.Round(nextX) - rectX;
                                g.FillRectangle(brush, rectX, y, rectW, height);
                            }

                            curX = nextX;
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
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public string ProductCode { get; set; }
        public decimal Price { get; set; }
        public int PrintQty { get; set; }
        public string ShelfLocation { get; set; }
    }
}

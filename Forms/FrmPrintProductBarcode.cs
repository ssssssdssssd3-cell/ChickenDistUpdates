using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using System.Collections.Generic;
using ChickenDist.Core;

namespace ChickenDist.Forms
{
    public class FrmPrintProductBarcode : Form
    {
        private int _productID;
        private string _productName;
        private string _productCode;
        private string _internationalCode;
        
        private ComboBox cboBarcodeType;
        private NumericUpDown nudPrintQty;
        private NumericUpDown nudPrice;
        private ComboBox cboPrinters;
        private ComboBox cboBarcodeTemplate;
        private ComboBox cboBarcodeEncoding;
        private CheckBox chkPrintPrice;
        private CheckBox chkPrintCompanyName;
        private Button btnPrint;
        private Button btnPreview;
        private Button btnCancel;

        // Print state tracking
        private string _selectedBarcode;
        private int _printQty = 1;
        private decimal _printedPrice;
        private int _printedLabelsCount = 0;

        private string _shelfLocation;

        public FrmPrintProductBarcode(int productID, string productName, string productCode, string internationalCode, decimal salePrice, string shelfLocation)
        {
            _productID = productID;
            _productName = productName;
            _productCode = productCode;
            _internationalCode = internationalCode;
            _shelfLocation = shelfLocation;

            InitializeComponent(salePrice);
        }

        private void InitializeComponent(decimal salePrice)
        {
            this.Text = "🏷️ طباعة باركود صنف";
            this.Size = new Size(500, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;

            // Title Bar
            var pnlTop = Theme.MakeTitleBar("🏷️ طباعة باركود الصنف", _productName);
            this.Controls.Add(pnlTop);

            int y = 90;

            // Barcode Type Selection
            this.Controls.Add(new Label { Text = "الرمز المطلوب طباعته:", Location = new Point(20, y + 4), AutoSize = true, ForeColor = Theme.TextMain });
            cboBarcodeType = new ComboBox
            {
                Location = new Point(180, y),
                Width = 280,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                FlatStyle = FlatStyle.Flat
            };
            cboBarcodeType.Items.Add($"كود الصنف الافتراضي ({_productCode})");
            if (!string.IsNullOrWhiteSpace(_internationalCode))
            {
                cboBarcodeType.Items.Add($"الكود الدولي / باركود المصنع ({_internationalCode})");
                cboBarcodeType.SelectedIndex = 1; // Default to international if available
            }
            else
            {
                cboBarcodeType.SelectedIndex = 0;
            }
            this.Controls.Add(cboBarcodeType);
            y += 40;

            // Print Quantity
            this.Controls.Add(new Label { Text = "عدد الملصقات:", Location = new Point(20, y + 4), AutoSize = true, ForeColor = Theme.TextMain });
            nudPrintQty = new NumericUpDown
            {
                Location = new Point(180, y),
                Width = 280,
                Minimum = 1,
                Maximum = 1000,
                Value = 1,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(nudPrintQty);
            y += 40;

            // Sale Price
            this.Controls.Add(new Label { Text = "سعر البيع المطبوع:", Location = new Point(20, y + 4), AutoSize = true, ForeColor = Theme.TextMain });
            nudPrice = new NumericUpDown
            {
                Location = new Point(180, y),
                Width = 280,
                Minimum = 0,
                Maximum = 999999,
                DecimalPlaces = 2,
                Value = salePrice,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(nudPrice);
            y += 40;

            // Printer Selection
            this.Controls.Add(new Label { Text = "اختر الطابعة:", Location = new Point(20, y + 4), AutoSize = true, ForeColor = Theme.TextMain });
            cboPrinters = new ComboBox
            {
                Location = new Point(180, y),
                Width = 280,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                FlatStyle = FlatStyle.Flat
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
            y += 40;

            // Sticker Template
            this.Controls.Add(new Label { Text = "شكل ملصق الباركود:", Location = new Point(20, y + 4), AutoSize = true, ForeColor = Theme.TextMain });
            cboBarcodeTemplate = new ComboBox
            {
                Location = new Point(180, y),
                Width = 280,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                FlatStyle = FlatStyle.Flat
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
            y += 40;

            // Barcode Encoding
            this.Controls.Add(new Label { Text = "نوع تشفير الباركود:", Location = new Point(20, y + 4), AutoSize = true, ForeColor = Theme.TextMain });
            cboBarcodeEncoding = new ComboBox
            {
                Location = new Point(180, y),
                Width = 280,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                FlatStyle = FlatStyle.Flat
            };
            cboBarcodeEncoding.Items.AddRange(new object[]
            {
                "Code 128 (موصى به - سريع وسهل القراءة)",
                "Code 39 (أحادي عريض)"
            });
            cboBarcodeEncoding.SelectedIndex = AppConfig.BarcodeEncoding == "Code39" ? 1 : 0;
            this.Controls.Add(cboBarcodeEncoding);
            y += 40;

            // Checkboxes
            chkPrintPrice = new CheckBox
            {
                Text = "طباعة السعر على الملصق",
                Location = new Point(180, y),
                AutoSize = true,
                ForeColor = Theme.TextMain,
                Checked = true
            };
            this.Controls.Add(chkPrintPrice);

            chkPrintCompanyName = new CheckBox
            {
                Text = "طباعة اسم المؤسسة",
                Location = new Point(180, y + 25),
                AutoSize = true,
                ForeColor = Theme.TextMain,
                Checked = true
            };
            this.Controls.Add(chkPrintCompanyName);
            y += 65;

            // Buttons
            btnPrint = Theme.MakeButton("🖨️ طباعة مباشرة", 20, y, 140, 36, Theme.Success);
            btnPrint.Click += (s, e) => StartPrintJob(false);
            this.Controls.Add(btnPrint);

            btnPreview = Theme.MakeButton("معاينة 👁️", 170, y, 110, 36, Theme.Accent);
            btnPreview.Click += (s, e) => StartPrintJob(true);
            this.Controls.Add(btnPreview);

            btnCancel = Theme.MakeButton("إلغاء ↩", 290, y, 110, 36, Color.FromArgb(70, 80, 95));
            btnCancel.Click += (s, e) => this.Close();
            this.Controls.Add(btnCancel);

            Theme.ApplyFormRTL(this);
        }

        private void StartPrintJob(bool isPreview)
        {
            // Determine code to print
            if (cboBarcodeType.SelectedIndex == 1 && !string.IsNullOrWhiteSpace(_internationalCode))
            {
                _selectedBarcode = _internationalCode.Trim();
            }
            else
            {
                _selectedBarcode = _productCode.Trim();
            }

            _printQty = (int)nudPrintQty.Value;
            _printedPrice = nudPrice.Value;
            _printedLabelsCount = 0;

            if (string.IsNullOrEmpty(_selectedBarcode))
            {
                MessageBox.Show("الرمز المحدد فارغ. لا يمكن الطباعة.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var pd = new PrintDocument();
                
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

                if (isPreview)
                {
                    var prev = new PrintPreviewDialog
                    {
                        Document = pd,
                        Width = 450,
                        Height = 400,
                        Text = "معاينة ملصق الباركود"
                    };
                    prev.ShowDialog(this);
                }
                else
                {
                    pd.Print();
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("فشل بدء عملية الطباعة:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Pd_PrintPage(object sender, PrintPageEventArgs e)
        {
            if (_printedLabelsCount >= _printQty)
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

            string labelType = (AppConfig.BarcodeStickerSize == "38x26") ? "Split" : "Full";
            int labelsPerRow = (labelType == "Split") ? 2 : 1;

            float pageWidth = e.PageBounds.Width;
            float pageHeight = e.PageBounds.Height;
            float leftMargin = 5;
            float topMargin = 5;

            float labelWidth = labelType == "Full" ? pageWidth : (pageWidth / labelsPerRow);
            float labelHeight = pageHeight - (topMargin * 2);

            bool isSmallSticker = (AppConfig.BarcodeStickerSize == "38x26");

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
                if (_printedLabelsCount >= _printQty)
                    break;

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
                    g.DrawString(_productName, fNameLarge, Brushes.Black, new RectangleF(x + 2, y, w - 4, isSmallSticker ? 22 : 30), center);
                    y += isSmallSticker ? 24 : 32;
                    if (chkPrintPrice.Checked)
                    {
                        g.DrawString($"{_printedPrice:N2} ج", fPriceLarge, Brushes.DarkRed, new RectangleF(x, y, w, isSmallSticker ? 18 : 24), center);
                        y += isSmallSticker ? 20 : 26;
                    }
                    if (!string.IsNullOrWhiteSpace(_shelfLocation))
                    {
                        g.DrawString($"الرف / مكان الصنف: {_shelfLocation}", fLocationLarge, Brushes.Black, new RectangleF(x, y, w, isSmallSticker ? 12 : 16), center);
                    }
                }
                else if (template == "Small")
                {
                    g.DrawString(_productName, new Font("Arial", isSmallSticker ? 6f : 6.5f, FontStyle.Bold), Brushes.Black, new RectangleF(x + 2, y, w - 4, isSmallSticker ? 12 : 16), center);
                    y += isSmallSticker ? 12 : 16;

                    float barcodeHeight = isSmallSticker ? 22 : 32;
                    float barcodeX = x + (w - (w - 20)) / 2;
                    if (isCode128)
                        DrawCode128(g, _selectedBarcode, barcodeX, y, w - 20, barcodeHeight);
                    else
                        DrawCode39(g, _selectedBarcode, barcodeX, y, w - 20, barcodeHeight);
                    y += barcodeHeight + 2;

                    g.DrawString(_selectedBarcode, new Font("Courier New", isSmallSticker ? 6f : 6.5f), Brushes.Black, new RectangleF(x, y, w, isSmallSticker ? 8 : 10), center);
                    y += isSmallSticker ? 8 : 10;

                    if (chkPrintPrice.Checked)
                    {
                        g.DrawString($"{_printedPrice:N2} ج", fPrice, Brushes.DarkRed, new RectangleF(x, y, w, isSmallSticker ? 10 : 12), center);
                    }
                }
                else if (template == "PriceHeavy")
                {
                    if (chkPrintPrice.Checked)
                    {
                        g.DrawString($"{_printedPrice:N2} ج", fPriceLarge, Brushes.DarkRed, new RectangleF(x + 5, y, w - 10, isSmallSticker ? 18 : 22), center);
                        y += isSmallSticker ? 20 : 24;
                    }
                    g.DrawString(_productName, fName, Brushes.Black, new RectangleF(x + 2, y, w - 4, isSmallSticker ? 14 : 18), center);
                    y += isSmallSticker ? 14 : 18;

                    float barcodeHeight = isSmallSticker ? 20 : 30;
                    float barcodeX = x + (w - (w - 20)) / 2;
                    if (isCode128)
                        DrawCode128(g, _selectedBarcode, barcodeX, y, w - 20, barcodeHeight);
                    else
                        DrawCode39(g, _selectedBarcode, barcodeX, y, w - 20, barcodeHeight);
                    y += barcodeHeight + 2;

                    g.DrawString(_selectedBarcode, fCode, Brushes.Black, new RectangleF(x + 5, y, w / 2 - 5, isSmallSticker ? 10 : 12), leftFormat);
                    if (!string.IsNullOrWhiteSpace(_shelfLocation))
                    {
                        g.DrawString($"الرف: {_shelfLocation}", fLocation, Brushes.Black, new RectangleF(x + w / 2, y, w / 2 - 5, isSmallSticker ? 10 : 12), rightFormat);
                    }
                }
                else
                {
                    if (chkPrintCompanyName.Checked)
                    {
                        g.DrawString(AppConfig.CompanyName, fCompany, Brushes.Black, new RectangleF(x, y, w, isSmallSticker ? 10 : 12), center);
                        y += isSmallSticker ? 10 : 12;
                    }
                    g.DrawString(_productName, fName, Brushes.Black, new RectangleF(x + 2, y, w - 4, isSmallSticker ? 16 : 24), center);
                    y += isSmallSticker ? 16 : 24;

                    float barcodeHeight = isSmallSticker ? 24 : 36;
                    float barcodeX = x + (w - (w - 20)) / 2;
                    if (isCode128)
                        DrawCode128(g, _selectedBarcode, barcodeX, y, w - 20, barcodeHeight);
                    else
                        DrawCode39(g, _selectedBarcode, barcodeX, y, w - 20, barcodeHeight);
                    y += barcodeHeight + 2;

                    g.DrawString(_selectedBarcode, fCode, Brushes.Black, new RectangleF(x, y, w, isSmallSticker ? 10 : 12), center);
                    y += isSmallSticker ? 10 : 12;

                    float bottomY = y;
                    if (chkPrintPrice.Checked)
                    {
                        g.DrawString($"السعر: {_printedPrice:N2} ج", fPrice, Brushes.DarkRed, new RectangleF(x + 5, bottomY, w / 2 - 5, isSmallSticker ? 11 : 14), leftFormat);
                    }
                    if (!string.IsNullOrWhiteSpace(_shelfLocation))
                    {
                        g.DrawString($"الرف: {_shelfLocation}", fLocation, Brushes.Black, new RectangleF(x + w / 2, bottomY, w / 2 - 5, isSmallSticker ? 11 : 14), rightFormat);
                    }
                }

                _printedLabelsCount++;
            }

            e.HasMorePages = (_printedLabelsCount < _printQty);
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
                float maxModuleWidth = (width < 140f) ? 0.95f : 1.15f; 
                if (moduleWidth > maxModuleWidth)
                {
                    moduleWidth = maxModuleWidth;
                }
                if (moduleWidth < 0.5f) moduleWidth = 0.5f;

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

        public static void DrawCode39(Graphics g, string code, float x, float y, float width, float height)
        {
            try
            {
                string textToEncode = "*" + code.ToUpper().Trim() + "*";
                
                // Set GDI+ options for crisp, aliased rendering (perfect for barcodes)
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.None;

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

                float totalUnits = textToEncode.Length * 16;
                float moduleWidth = width / totalUnits;
                
                // Cap module width to prevent extremely fat bleeding bars for short codes
                float maxModuleWidth = (width < 140f) ? 0.90f : 1.10f; 
                if (moduleWidth > maxModuleWidth)
                {
                    moduleWidth = maxModuleWidth;
                }
                if (moduleWidth < 0.5f) moduleWidth = 0.5f;

                // Center the barcode
                float actualBarcodeWidth = totalUnits * moduleWidth;
                float curX = x + (width - actualBarcodeWidth) / 2;

                using (var brush = new SolidBrush(Color.Black))
                {
                    foreach (char c in textToEncode)
                    {
                        string pattern;
                        if (!map.TryGetValue(c, out pattern))
                            pattern = map['*'];

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

                        curX += moduleWidth; // Inter-character gap
                    }
                }
            }
            catch { }
        }
    }
}

using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    public class FrmPrintProductBarcode : Form
    {
        private int _productID;
        private string _productName;
        private string _productCode;
        private string _internationalCode;
        private List<string> _imeiList = new List<string>();
        private List<string> _availableStockIMEIs = new List<string>();
        private List<string> _activeBatchSerials = null;
        private int _totalLabelsToPrint = 0;
        
        private ComboBox cboBarcodeType;
        private NumericUpDown nudPrintQty;
        private NumericUpDown nudPrice;
        private ComboBox cboPrinters;
        private ComboBox cboBarcodeTemplate;
        private ComboBox cboBarcodeEncoding;
        private CheckBox chkPrintPrice;
        private CheckBox chkPrintCompanyName;
        private CheckBox chkPrintCategoryInsteadOfName;
        private Button btnPrint;
        private Button btnPreview;
        private Button btnCancel;

        // Print state tracking
        private string _selectedBarcode;
        private int _printQty = 1;
        private decimal _printedPrice;
        private int _printedLabelsCount = 0;
        private string _printTemplate = "Standard";
        private string _printEncoding = "Code128";
        private bool _printPriceFlag = true;
        private bool _printCompanyNameFlag = true;
        private bool _printCategoryFlag = false;
        private string _printedTitleText = "";
        private string _printCompanyNameText = "";
        private string _printBarcodeStickerSize = "50x30";

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

        public FrmPrintProductBarcode(int productID, string productName, List<string> imeis, decimal salePrice = 0m, string shelfLocation = "")
        {
            _productID = productID;
            _productName = productName;
            _imeiList = imeis != null ? new List<string>(imeis.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim())) : new List<string>();
            _shelfLocation = shelfLocation;

            if (salePrice <= 0m && productID > 0)
            {
                try
                {
                    object sp = DbHelper.Scalar("SELECT SalePrice FROM Products WHERE ProductID = @id", DbHelper.P("@id", productID));
                    if (sp != null && sp != DBNull.Value) salePrice = Convert.ToDecimal(sp);
                }
                catch { }
            }

            try
            {
                var dtCode = DbHelper.Query("SELECT ProductCode, InternationalBarcode FROM Products WHERE ProductID = @id", DbHelper.P("@id", productID));
                if (dtCode.Rows.Count > 0)
                {
                    _productCode = dtCode.Rows[0]["ProductCode"]?.ToString() ?? "";
                    _internationalCode = dtCode.Rows[0]["InternationalBarcode"]?.ToString() ?? "";
                }
            }
            catch { }

            InitializeComponent(salePrice);
        }

        private void InitializeComponent(decimal salePrice)
        {
            this.Text = "طباعة باركود صنف";
            this.Size = new Size(540, 585);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;

            // Title Bar
            var pnlTop = Theme.MakeTitleBar("طباعة باركود الصنف", _productName);
            this.Controls.Add(pnlTop);

            var pnlContent = new Panel
            {
                Location = new Point(15, 60),
                Size = new Size(495, 465),
                BackColor = Theme.BgCard,
                Padding = new Padding(15)
            };
            this.Controls.Add(pnlContent);

            int y = 15;
            int inputX = 180;
            int inputW = 285;

            // Barcode Type Selection
            pnlContent.Controls.Add(new Label { Text = "الرمز المطلوب طباعته:", Location = new Point(15, y + 4), AutoSize = true, ForeColor = Theme.TextMain, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) });
            cboBarcodeType = new ComboBox
            {
                Location = new Point(inputX, y),
                Width = inputW,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(30, 41, 59),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };

            if (_imeiList.Count > 0)
            {
                cboBarcodeType.Items.Add($"طباعة مخصصة لكل سيريال ({_imeiList.Count} جهاز)");
                foreach (var imei in _imeiList)
                {
                    cboBarcodeType.Items.Add($"سيريال: {imei}");
                }
                if (!string.IsNullOrWhiteSpace(_productCode))
                    cboBarcodeType.Items.Add($"كود الصنف الافتراضي ({_productCode})");
                if (!string.IsNullOrWhiteSpace(_internationalCode))
                    cboBarcodeType.Items.Add($"الكود الدولي ({_internationalCode})");
                cboBarcodeType.SelectedIndex = 0;
            }
            else
            {
                cboBarcodeType.Items.Add($"كود الصنف الافتراضي ({_productCode})");
                if (!string.IsNullOrWhiteSpace(_internationalCode))
                {
                    cboBarcodeType.Items.Add($"الكود الدولي / باركود المصنع ({_internationalCode})");
                }
                if (_productID > 0)
                {
                    try
                    {
                        var stockSerials = PurchaseDAL.GetAvailableIMEIsForProduct(_productID);
                        if (stockSerials != null && stockSerials.Count > 0)
                        {
                            _availableStockIMEIs = stockSerials;
                            cboBarcodeType.Items.Add($"طباعة جميع سيريالات المخزون المتاحة ({_availableStockIMEIs.Count} جهاز)");
                            foreach (var imei in _availableStockIMEIs)
                            {
                                cboBarcodeType.Items.Add($"سيريال متاح: {imei}");
                            }
                        }
                    }
                    catch { }
                }

                if (AppConfig.BusinessType == "Mobiles" && _availableStockIMEIs.Count > 0)
                {
                    int bIdx = cboBarcodeType.FindString("طباعة جميع سيريالات");
                    cboBarcodeType.SelectedIndex = bIdx >= 0 ? bIdx : 0;
                }
                else if (!string.IsNullOrWhiteSpace(_internationalCode))
                {
                    cboBarcodeType.SelectedIndex = 1;
                }
                else
                {
                    cboBarcodeType.SelectedIndex = 0;
                }
            }
            pnlContent.Controls.Add(cboBarcodeType);
            y += 42;

            // Print Quantity
            pnlContent.Controls.Add(new Label { Text = "عدد الملصقات:", Location = new Point(15, y + 4), AutoSize = true, ForeColor = Theme.TextMain, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) });
            nudPrintQty = new NumericUpDown
            {
                Location = new Point(inputX, y),
                Width = inputW,
                Minimum = 1,
                Maximum = 1000,
                Value = 1,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(30, 41, 59),
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                BorderStyle = BorderStyle.FixedSingle
            };
            pnlContent.Controls.Add(nudPrintQty);
            y += 42;

            // Sale Price
            pnlContent.Controls.Add(new Label { Text = "سعر البيع المطبوع:", Location = new Point(15, y + 4), AutoSize = true, ForeColor = Theme.TextMain, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) });
            nudPrice = new NumericUpDown
            {
                Location = new Point(inputX, y),
                Width = inputW,
                Minimum = 0,
                Maximum = 999999,
                DecimalPlaces = 2,
                Value = salePrice,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(30, 41, 59),
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                BorderStyle = BorderStyle.FixedSingle
            };
            pnlContent.Controls.Add(nudPrice);
            y += 42;

            // Printer Selection
            pnlContent.Controls.Add(new Label { Text = "اختر الطابعة:", Location = new Point(15, y + 4), AutoSize = true, ForeColor = Theme.TextMain, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) });
            cboPrinters = new ComboBox
            {
                Location = new Point(inputX, y),
                Width = inputW,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(30, 41, 59),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
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
            pnlContent.Controls.Add(cboPrinters);
            y += 42;

            // Sticker Template
            pnlContent.Controls.Add(new Label { Text = "شكل ملصق الباركود:", Location = new Point(15, y + 4), AutoSize = true, ForeColor = Theme.TextMain, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) });
            cboBarcodeTemplate = new ComboBox
            {
                Location = new Point(inputX, y),
                Width = inputW,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(30, 41, 59),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            cboBarcodeTemplate.Items.AddRange(new object[]
            {
                "ملصق أجهزة وسيريالات الموبايل (اسم المكان كبير + IMEI)",
                "كود الصنف علي العرض",
                "الافتراضي (اسم صنف + سعر + باركود)",
                "سعر بارز (سعر كبير + باركود)",
                "ملصق صغير (سعر وباركود فقط)",
                "ملصق الرف (اسم صنف وسعر كبير - بدون باركود)",
                "اسم صنف كبير + باركود (بدون سعر)"
            });

            bool isMobileMode = _imeiList.Count > 0 || _availableStockIMEIs.Count > 0 || AppConfig.BusinessType == "Mobiles" || AppConfig.BarcodeTemplate == "MobileIMEI" || AppConfig.BarcodeTemplate == "CodeOnSide";
            cboBarcodeTemplate.SelectedItem = AppConfig.BarcodeTemplate == "CodeOnSide" ? "كود الصنف علي العرض"
                                            : (isMobileMode && (AppConfig.BarcodeTemplate == "MobileIMEI" || AppConfig.BarcodeTemplate == "Standard" || string.IsNullOrEmpty(AppConfig.BarcodeTemplate)))
                                            ? "ملصق أجهزة وسيريالات الموبايل (اسم المكان كبير + IMEI)"
                                            : AppConfig.BarcodeTemplate == "MobileIMEI" ? "ملصق أجهزة وسيريالات الموبايل (اسم المكان كبير + IMEI)"
                                            : AppConfig.BarcodeTemplate == "PriceHeavy" ? "سعر بارز (سعر كبير + باركود)"
                                            : AppConfig.BarcodeTemplate == "Small" ? "ملصق صغير (سعر وباركود فقط)"
                                            : AppConfig.BarcodeTemplate == "Shelf" ? "ملصق الرف (اسم صنف وسعر كبير - بدون باركود)"
                                            : (AppConfig.BarcodeTemplate == "NoPrice" || AppConfig.BarcodeTemplate == "NoPriceBigName") ? "اسم صنف كبير + باركود (بدون سعر)"
                                            : "الافتراضي (اسم صنف + سعر + باركود)";
            if (cboBarcodeTemplate.SelectedIndex == -1) cboBarcodeTemplate.SelectedIndex = 0;
            pnlContent.Controls.Add(cboBarcodeTemplate);
            y += 42;

            // Barcode Encoding
            pnlContent.Controls.Add(new Label { Text = "نوع تشفير الباركود:", Location = new Point(15, y + 4), AutoSize = true, ForeColor = Theme.TextMain, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) });
            cboBarcodeEncoding = new ComboBox
            {
                Location = new Point(inputX, y),
                Width = inputW,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(30, 41, 59),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            var encList = BarcodeEngine.GetAvailableEncodings();
            foreach (var enc in encList)
            {
                cboBarcodeEncoding.Items.Add(enc.Item2);
            }
            int defIdx = encList.FindIndex(x => x.Item1 == AppConfig.BarcodeEncoding);
            cboBarcodeEncoding.SelectedIndex = defIdx >= 0 ? defIdx : 0;
            pnlContent.Controls.Add(cboBarcodeEncoding);
            y += 42;

            // Checkboxes
            chkPrintPrice = new CheckBox
            {
                Text = "طباعة السعر على الملصق",
                Location = new Point(inputX, y),
                AutoSize = true,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Checked = AppConfig.BarcodePrintPrice
            };
            chkPrintPrice.CheckedChanged += (s, e) =>
            {
                _printPriceFlag = chkPrintPrice.Checked;
            };
            pnlContent.Controls.Add(chkPrintPrice);

            chkPrintCompanyName = new CheckBox
            {
                Text = "طباعة اسم المؤسسة",
                Location = new Point(inputX, y + 24),
                AutoSize = true,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Checked = true
            };
            pnlContent.Controls.Add(chkPrintCompanyName);

            chkPrintCategoryInsteadOfName = new CheckBox
            {
                Text = "طباعة التصنيف بدلاً من اسم الصنف",
                Location = new Point(inputX, y + 48),
                AutoSize = true,
                ForeColor = Theme.Accent,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Checked = AppConfig.BarcodePrintCategoryInsteadOfName
            };
            pnlContent.Controls.Add(chkPrintCategoryInsteadOfName);
            y += 84;

            // Buttons
            btnPrint = Theme.MakeButton("طباعة مباشرة", 15, y, 150, 40, Theme.Success);
            btnPrint.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            btnPrint.Click += (s, e) => StartPrintJob(false);
            pnlContent.Controls.Add(btnPrint);

            btnPreview = Theme.MakeButton("معاينة", 175, y, 140, 40, Theme.Accent);
            btnPreview.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            btnPreview.Click += (s, e) => StartPrintJob(true);
            pnlContent.Controls.Add(btnPreview);

            btnCancel = Theme.MakeButton("إلغاء", 325, y, 140, 40, Color.FromArgb(100, 116, 139));
            btnCancel.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            btnCancel.Click += (s, e) => this.Close();
            pnlContent.Controls.Add(btnCancel);

            Theme.ApplyFormRTL(this);
        }

        private void StartPrintJob(bool isPreview)
        {
            string selectedType = cboBarcodeType.SelectedItem?.ToString() ?? "";
            _activeBatchSerials = null;

            if (selectedType.StartsWith("طباعة مخصصة لكل سيريال") && _imeiList.Count > 0)
            {
                _activeBatchSerials = _imeiList;
                _selectedBarcode = _imeiList[0];
            }
            else if (selectedType.StartsWith("طباعة جميع سيريالات") && _availableStockIMEIs.Count > 0)
            {
                _activeBatchSerials = _availableStockIMEIs;
                _selectedBarcode = _availableStockIMEIs[0];
            }
            else if (selectedType.StartsWith("سيريال: "))
            {
                _selectedBarcode = selectedType.Substring("سيريال: ".Length).Trim();
            }
            else if (selectedType.StartsWith("سيريال متاح: "))
            {
                _selectedBarcode = selectedType.Substring("سيريال متاح: ".Length).Trim();
            }
            else if (selectedType.Contains("الكود الدولي") && !string.IsNullOrWhiteSpace(_internationalCode))
            {
                _selectedBarcode = _internationalCode.Trim();
            }
            else
            {
                _selectedBarcode = !string.IsNullOrWhiteSpace(_productCode) ? _productCode.Trim() : (_imeiList.Count > 0 ? _imeiList[0] : "");
            }

            _printQty = (int)nudPrintQty.Value;
            _printedPrice = nudPrice.Value;
            _printedLabelsCount = 0;

            if (_activeBatchSerials != null && _activeBatchSerials.Count > 0)
            {
                _totalLabelsToPrint = _activeBatchSerials.Count * _printQty;
            }
            else
            {
                _totalLabelsToPrint = _printQty;
            }

            if (string.IsNullOrEmpty(_selectedBarcode))
            {
                MessageBox.Show("الرمز المحدد فارغ. لا يمكن الطباعة.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string selectedTmpl = cboBarcodeTemplate.SelectedItem?.ToString() ?? "";
            if (selectedTmpl.Contains("كود الصنف علي العرض") || selectedTmpl.Contains("كود الصنف على العرض") || selectedTmpl == "CodeOnSide")
                _printTemplate = "CodeOnSide";
            else if (selectedTmpl.Contains("MobileIMEI") || selectedTmpl.Contains("الموبايل") || selectedTmpl.Contains("IMEI"))
                _printTemplate = "MobileIMEI";
            else if (selectedTmpl.Contains("سعر بارز"))
                _printTemplate = "PriceHeavy";
            else if (selectedTmpl.Contains("ملصق صغير"))
                _printTemplate = "Small";
            else if (selectedTmpl.Contains("ملصق الرف"))
                _printTemplate = "Shelf";
            else if (selectedTmpl.Contains("بدون سعر"))
                _printTemplate = "NoPrice";
            else
                _printTemplate = "Standard";

            if (cboBarcodeEncoding.SelectedIndex >= 0)
            {
                var encList = BarcodeEngine.GetAvailableEncodings();
                if (cboBarcodeEncoding.SelectedIndex < encList.Count)
                {
                    _printEncoding = encList[cboBarcodeEncoding.SelectedIndex].Item1;
                    AppConfig.BarcodeEncoding = _printEncoding;
                }
            }
            _printPriceFlag = chkPrintPrice.Checked;
            _printCompanyNameFlag = chkPrintCompanyName.Checked;
            _printCategoryFlag = chkPrintCategoryInsteadOfName != null && chkPrintCategoryInsteadOfName.Checked;
            AppConfig.BarcodePrintCategoryInsteadOfName = _printCategoryFlag;
            _printedTitleText = BarcodeEngine.GetBarcodeTitleText(_productID, _productName, null, _printCategoryFlag);
            _printCompanyNameText = AppConfig.CompanyName;
            _printBarcodeStickerSize = AppConfig.BarcodeStickerSize;

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

                AppConfig.SetPaperSize(pd, _printBarcodeStickerSize);

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
                    AppConfig.PrintInBackground(pd);
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
            if (_printedLabelsCount >= _totalLabelsToPrint)
            {
                e.HasMorePages = false;
                return;
            }

            var g = e.Graphics;

            string template = _printTemplate;
            string titleToPrint = !string.IsNullOrWhiteSpace(_printedTitleText) ? _printedTitleText : _productName;

            string labelType = (_printBarcodeStickerSize == "38x26_double") ? "Split" : "Full";
            int labelsPerRow = (labelType == "Split") ? 2 : 1;
 
            float pageWidth = e.PageBounds.Width;
            float pageHeight = e.PageBounds.Height;
            float leftMargin = 5;
            float topMargin = 5;
 
            float labelWidth = labelType == "Full" ? pageWidth : (pageWidth / labelsPerRow);
            float labelHeight = pageHeight - (topMargin * 2);
 
            bool isSmallSticker = (_printBarcodeStickerSize == "38x26" || _printBarcodeStickerSize == "38x26_double");

            var fCompany  = new Font("Arial", isSmallSticker ? 6.5f : 8f, FontStyle.Bold);
            var fName     = new Font("Arial", isSmallSticker ? 6.5f : 7.5f, FontStyle.Bold);
            var fPrice    = new Font("Arial", isSmallSticker ? 7.5f : 8.5f, FontStyle.Bold);
            var fCode     = new Font("Courier New", isSmallSticker ? 6.5f : 7.5f, FontStyle.Regular);
            var fLocation = new Font("Arial", isSmallSticker ? 6.5f : 7.5f, FontStyle.Bold);

            var fPriceLarge    = new Font("Arial", isSmallSticker ? 11f : 14f, FontStyle.Bold);
            var fNameLarge     = new Font("Arial", isSmallSticker ? 8.5f : 10f, FontStyle.Bold);
            var fLocationLarge = new Font("Arial", isSmallSticker ? 7.5f : 9f, FontStyle.Bold);

            var center = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap | StringFormatFlags.NoClip };
            var leftFormat = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap | StringFormatFlags.NoClip };
            var rightFormat = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap | StringFormatFlags.NoClip };

            // Print labels on the current page row
            for (int itemIndex = 0; itemIndex < labelsPerRow; itemIndex++)
            {
                if (_printedLabelsCount >= _totalLabelsToPrint)
                    break;

                string currentBarcode;
                if (_activeBatchSerials != null && _activeBatchSerials.Count > 0)
                {
                    int serialIndex = _printedLabelsCount / _printQty;
                    if (serialIndex >= _activeBatchSerials.Count) break;
                    currentBarcode = _activeBatchSerials[serialIndex];
                }
                else
                {
                    currentBarcode = _selectedBarcode;
                }

                int currentColumn = itemIndex % labelsPerRow;
                int currentRow = itemIndex / labelsPerRow;

                float startX = leftMargin + (currentColumn * labelWidth);
                float startY = topMargin + (currentRow * labelHeight);

                // Use local variables bounded to the label
                float x = startX;
                float y = startY + 2f;
                float w = labelWidth - (leftMargin * 2);

                if (template == "CodeOnSide")
                {
                    // نموذج مخصص: كود الصنف علي العرض (كود الصنف يميناً + كود المورد يساراً بالعرض بجانب الباركود)
                    string compName = !string.IsNullOrWhiteSpace(_printCompanyNameText) ? _printCompanyNameText : AppConfig.CompanyName;
                    if (_printCompanyNameFlag && !string.IsNullOrWhiteSpace(compName))
                    {
                        var fComp = new Font("Arial", isSmallSticker ? 7.5f : 9f, FontStyle.Bold);
                        float compH = isSmallSticker ? 12f : 15f;
                        DrawFittedText(g, compName, fComp, Brushes.Black, new RectangleF(x + 2, y, w - 4, compH), center, 5.5f);
                        y += compH + 1f;
                    }

                    var fDeviceTitle = new Font("Arial", isSmallSticker ? 6.5f : 8f, FontStyle.Bold);
                    float titleH = isSmallSticker ? 13f : 16f;
                    DrawFittedText(g, titleToPrint, fDeviceTitle, Brushes.Black, new RectangleF(x + 1, y, w - 2, titleH), center, 5.5f);
                    y += titleH + 2f;

                    float barcodeHeight = isSmallSticker ? 24f : 30f;
                    float sideColWidth = isSmallSticker ? 10f : 13f;
                    float quietGap = isSmallSticker ? 12f : 15f;
                    float totalSide = sideColWidth + quietGap;

                    float barcodeX = x + totalSide;
                    float barcodeW = w - (totalSide * 2);

                    // الباركود الرئيسي في المنتصف (حسب التشفير المختار مع مسافة أمان عريضة)
                    string enc = !string.IsNullOrWhiteSpace(_printEncoding) ? _printEncoding : "Code128";
                    BarcodeEngine.DrawBarcode(g, currentBarcode, enc, barcodeX, y, barcodeW, barcodeHeight);

                    // كود الصنف على اليمين بالعرض
                    string rightCode = !string.IsNullOrWhiteSpace(_productCode) ? _productCode : "";
                    var fSide = new Font("Segoe UI", isSmallSticker ? 6f : 7f, FontStyle.Bold);
                    RectangleF rightRect = new RectangleF(x + w - sideColWidth - 1, y, sideColWidth, barcodeHeight);
                    BarcodeEngine.DrawRotatedFittedText(g, rightCode, fSide, Brushes.Black, rightRect, true);

                    // كود المورد على الشمال بالعرض
                    string supCode = "";
                    try
                    {
                        supCode = PurchaseDAL.GetProductSupplierInfo(_productID, currentBarcode).SupplierCode;
                    }
                    catch { }
                    if (!string.IsNullOrWhiteSpace(supCode))
                    {
                        RectangleF leftRect = new RectangleF(x + 1, y, sideColWidth, barcodeHeight);
                        BarcodeEngine.DrawRotatedFittedText(g, supCode, fSide, Brushes.Black, leftRect, true);
                    }

                    y += barcodeHeight + 2f;

                    var fIMEIText = new Font("Consolas", isSmallSticker ? 7.5f : 8.5f, FontStyle.Bold);
                    string imeiDisplay = (currentBarcode.Length == 15 && currentBarcode.All(char.IsDigit)) ? $"IMEI: {currentBarcode}" : currentBarcode;
                    float imeiH = isSmallSticker ? 12f : 14f;

                    if (_printPriceFlag && _printedPrice > 0)
                    {
                        DrawFittedText(g, imeiDisplay, fIMEIText, Brushes.Black, new RectangleF(x + 2, y, w - 4, imeiH), center, 5.5f);
                        y += imeiH + 1f;
                        var fPriceSmall = new Font("Arial", isSmallSticker ? 7.5f : 9f, FontStyle.Bold);
                        float priceH = isSmallSticker ? 14f : 17f;
                        DrawFittedText(g, $"السعر: {_printedPrice:N0} ج", fPriceSmall, Brushes.DarkRed, new RectangleF(x + 2, y, w - 4, priceH), center, 6f);
                    }
                    else
                    {
                        float imeiAloneH = isSmallSticker ? 14f : 17f;
                        DrawFittedText(g, imeiDisplay, fIMEIText, Brushes.Black, new RectangleF(x + 2, y, w - 4, imeiAloneH), center, 6f);
                    }
                }
                else if (template == "MobileIMEI")
                {
                    string compName = !string.IsNullOrWhiteSpace(_printCompanyNameText) ? _printCompanyNameText : AppConfig.CompanyName;
                    if (_printCompanyNameFlag && !string.IsNullOrWhiteSpace(compName))
                    {
                        // 1. اسم المكان كبير وواضح وبارز في أعلى الملصق بخط متناسق مع الحواف ومحمي من القص
                        var fCompanyHuge = new Font("Arial", isSmallSticker ? 8.5f : 10f, FontStyle.Bold);
                        float compH = isSmallSticker ? 18f : 22f;
                        DrawFittedText(g, compName, fCompanyHuge, Brushes.Black, new RectangleF(x + 2, y, w - 4, compH), center, 5.5f);
                        y += compH + 1f;
                    }

                    // 2. اسم الجهاز والموديل
                    var fDeviceTitle = new Font("Arial", isSmallSticker ? 7.5f : 9f, FontStyle.Bold);
                    float titleH = isSmallSticker ? 16f : 19f;
                    DrawFittedText(g, titleToPrint, fDeviceTitle, Brushes.Black, new RectangleF(x + 2, y, w - 4, titleH), center, 5.5f);
                    y += titleH + 2f;

                    // 3. رسم باركود الـ IMEI بدقة عالية ومسافات حواف مريحة
                    float barcodeHeight = isSmallSticker ? 24f : 30f;
                    float barcodeMargin = isSmallSticker ? 8f : 12f;
                    float barcodeX = x + barcodeMargin;
                    float barcodeW = w - (barcodeMargin * 2);
                    string encM = !string.IsNullOrWhiteSpace(_printEncoding) ? _printEncoding : "Code128";
                    BarcodeEngine.DrawBarcode(g, currentBarcode, encM, barcodeX, y, barcodeW, barcodeHeight);
                    y += barcodeHeight + 2f;

                    // 4. كتابة كود الـ IMEI بوضوح + كود الصنف والمورد والسعر
                    var fIMEIText = new Font("Consolas", isSmallSticker ? 7.5f : 8.5f, FontStyle.Bold);
                    string imeiDisplay = (currentBarcode.Length == 15 && currentBarcode.All(char.IsDigit)) ? $"IMEI: {currentBarcode}" : $"S/N: {currentBarcode}";
                    float imeiH = isSmallSticker ? 12f : 14f;
                    DrawFittedText(g, imeiDisplay, fIMEIText, Brushes.Black, new RectangleF(x + 2, y, w - 4, imeiH), center, 6f);
                    y += imeiH + 1f;

                    string supCode = "";
                    try { supCode = PurchaseDAL.GetProductSupplierInfo(_productID, currentBarcode).SupplierCode; } catch { }
                    string codePart = !string.IsNullOrWhiteSpace(_productCode) ? $"كود: {_productCode}" : "";
                    string supPart = !string.IsNullOrWhiteSpace(supCode) ? $"مورد: {supCode}" : "";
                    string extraCodes = string.Join(" | ", new string[] { codePart, supPart }.Where(s => !string.IsNullOrEmpty(s)));

                    var fPriceSmall = new Font("Arial", isSmallSticker ? 7.5f : 9f, FontStyle.Bold);
                    float priceH = isSmallSticker ? 14f : 17f;

                    if (_printPriceFlag && _printedPrice > 0)
                    {
                        string priceStr = $"السعر: {_printedPrice:N0} ج";
                        string fullBottom = !string.IsNullOrEmpty(extraCodes) ? $"{priceStr}  •  {extraCodes}" : priceStr;
                        DrawFittedText(g, fullBottom, fPriceSmall, Brushes.DarkRed, new RectangleF(x + 2, y, w - 4, priceH), center, 5.5f);
                    }
                    else if (!string.IsNullOrEmpty(extraCodes))
                    {
                        DrawFittedText(g, extraCodes, fPriceSmall, Brushes.Black, new RectangleF(x + 2, y, w - 4, priceH), center, 5.5f);
                    }
                }
                else if (template == "Shelf")
                {
                    if (_printCompanyNameFlag)
                    {
                        DrawFittedText(g, _printCompanyNameText, fCompany, Brushes.Gray, new RectangleF(x + 2, y, w - 4, isSmallSticker ? 10 : 12), center, 5f);
                        y += isSmallSticker ? 11 : 14;
                    }
                    DrawFittedText(g, titleToPrint, fNameLarge, Brushes.Black, new RectangleF(x + 2, y, w - 4, isSmallSticker ? 22 : 30), center, 5.5f);
                    y += isSmallSticker ? 24 : 32;
                    if (_printPriceFlag)
                    {
                        DrawFittedText(g, $"{_printedPrice:N2} ج", fPriceLarge, Brushes.DarkRed, new RectangleF(x + 2, y, w - 4, isSmallSticker ? 18 : 24), center, 6f);
                        y += isSmallSticker ? 20 : 26;
                    }
                    if (!string.IsNullOrWhiteSpace(_shelfLocation))
                    {
                        g.DrawString($"الرف / مكان الصنف: {_shelfLocation}", fLocationLarge, Brushes.Black, new RectangleF(x, y, w, isSmallSticker ? 12 : 16), center);
                    }
                }
                else if (template == "Small")
                {
                    g.DrawString(titleToPrint, new Font("Arial", isSmallSticker ? 6f : 6.5f, FontStyle.Bold), Brushes.Black, new RectangleF(x + 2, y, w - 4, isSmallSticker ? 12 : 16), center);
                    y += isSmallSticker ? 12 : 16;

                    float barcodeHeight = isSmallSticker ? 22 : 32;
                    float barcodeX = x + (w - (w - 20)) / 2;
                    BarcodeEngine.DrawBarcode(g, currentBarcode, _printEncoding, barcodeX, y, w - 20, barcodeHeight);
                    y += barcodeHeight + 2;

                    g.DrawString(currentBarcode, new Font("Courier New", isSmallSticker ? 6f : 6.5f), Brushes.Black, new RectangleF(x, y, w, isSmallSticker ? 8 : 10), center);
                    y += isSmallSticker ? 8 : 10;

                    if (_printPriceFlag)
                    {
                        g.DrawString($"{_printedPrice:N2} ج", fPrice, Brushes.DarkRed, new RectangleF(x, y, w, isSmallSticker ? 10 : 12), center);
                    }
                }
                else if (template == "PriceHeavy")
                {
                    if (_printPriceFlag)
                    {
                        DrawFittedText(g, $"{_printedPrice:N2} ج", fPriceLarge, Brushes.DarkRed, new RectangleF(x + 2, y, w - 4, isSmallSticker ? 18 : 22), center, 6f);
                        y += isSmallSticker ? 20 : 24;
                    }
                    DrawFittedText(g, titleToPrint, fName, Brushes.Black, new RectangleF(x + 2, y, w - 4, isSmallSticker ? 14 : 18), center, 5.5f);
                    y += isSmallSticker ? 14 : 18;

                    float barcodeHeight = isSmallSticker ? 20 : 30;
                    float barcodeX = x + (w - (w - 20)) / 2;
                    BarcodeEngine.DrawBarcode(g, currentBarcode, _printEncoding, barcodeX, y, w - 20, barcodeHeight);
                    y += barcodeHeight + 2;

                    g.DrawString(currentBarcode, fCode, Brushes.Black, new RectangleF(x + 5, y, w / 2 - 5, isSmallSticker ? 10 : 12), leftFormat);
                    if (!string.IsNullOrWhiteSpace(_shelfLocation))
                    {
                        g.DrawString($"الرف: {_shelfLocation}", fLocation, Brushes.Black, new RectangleF(x + w / 2, y, w / 2 - 5, isSmallSticker ? 10 : 12), rightFormat);
                    }
                }
                else if (template == "NoPrice" || template == "NoPriceBigName")
                {
                    if (_printCompanyNameFlag && !string.IsNullOrWhiteSpace(_printCompanyNameText))
                    {
                        DrawFittedText(g, _printCompanyNameText, fCompany, Brushes.Black, new RectangleF(x + 2, y, w - 4, isSmallSticker ? 10 : 12), center, 5f);
                        y += isSmallSticker ? 10 : 12;
                    }

                    // اسم الصنف بخط كبير وبارز ومحمي من القص
                    DrawFittedText(g, titleToPrint, fNameLarge, Brushes.Black, new RectangleF(x + 2, y, w - 4, isSmallSticker ? 20 : 28), center, 5.5f);
                    y += isSmallSticker ? 20 : 28;

                    // رسم الباركود بدون سعر
                    float barcodeHeight = isSmallSticker ? 26 : 38;
                    float barcodeX = x + (w - (w - 16)) / 2;
                    BarcodeEngine.DrawBarcode(g, currentBarcode, _printEncoding, barcodeX, y, w - 16, barcodeHeight);
                    y += barcodeHeight + 2;

                    // كود الباركود ورقم الرف إن وجد
                    if (!string.IsNullOrWhiteSpace(_shelfLocation))
                    {
                        g.DrawString(currentBarcode, fCode, Brushes.Black, new RectangleF(x + 2, y, w / 2 - 2, isSmallSticker ? 10 : 12), leftFormat);
                        g.DrawString($"الرف: {_shelfLocation}", fLocation, Brushes.Black, new RectangleF(x + w / 2, y, w / 2 - 2, isSmallSticker ? 10 : 12), rightFormat);
                    }
                    else
                    {
                        g.DrawString(currentBarcode, fCode, Brushes.Black, new RectangleF(x, y, w, isSmallSticker ? 10 : 12), center);
                    }
                }
                else
                {
                    if (_printCompanyNameFlag && !string.IsNullOrWhiteSpace(_printCompanyNameText))
                    {
                        DrawFittedText(g, _printCompanyNameText, fCompany, Brushes.Black, new RectangleF(x + 2, y, w - 4, isSmallSticker ? 10 : 12), center, 5f);
                        y += isSmallSticker ? 10 : 12;
                    }
                    DrawFittedText(g, titleToPrint, fName, Brushes.Black, new RectangleF(x + 2, y, w - 4, isSmallSticker ? 16 : 24), center, 5.5f);
                    y += isSmallSticker ? 16 : 24;

                    float barcodeHeight = isSmallSticker ? 24 : 36;
                    float barcodeX = x + (w - (w - 20)) / 2;
                    BarcodeEngine.DrawBarcode(g, currentBarcode, _printEncoding, barcodeX, y, w - 20, barcodeHeight);
                    y += barcodeHeight + 2;

                    g.DrawString(currentBarcode, fCode, Brushes.Black, new RectangleF(x, y, w, isSmallSticker ? 10 : 12), center);
                    y += isSmallSticker ? 10 : 12;

                    float bottomY = y;
                    if (_printPriceFlag)
                    {
                        DrawFittedText(g, $"السعر: {_printedPrice:N2} ج", fPrice, Brushes.DarkRed, new RectangleF(x + 2, bottomY, w / 2 - 2, isSmallSticker ? 11 : 14), leftFormat, 5.5f);
                    }
                    if (!string.IsNullOrWhiteSpace(_shelfLocation))
                    {
                        g.DrawString($"الرف: {_shelfLocation}", fLocation, Brushes.Black, new RectangleF(x + w / 2, bottomY, w / 2 - 5, isSmallSticker ? 11 : 14), rightFormat);
                    }
                }

                _printedLabelsCount++;
            }

            e.HasMorePages = (_printedLabelsCount < _totalLabelsToPrint);
        }

        private static void DrawFittedText(Graphics g, string text, Font baseFont, Brush brush, RectangleF rect, StringFormat format, float minFontSize = 5f)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            float currentSize = baseFont.Size;
            Font fontToUse = baseFont;
            bool createdNew = false;
            try
            {
                while (currentSize > minFontSize)
                {
                    SizeF size = g.MeasureString(text, fontToUse, (int)rect.Width, format);
                    if (size.Width <= rect.Width && size.Height <= rect.Height + 2f)
                        break;
                    currentSize -= 0.5f;
                    Font nextFont = new Font(baseFont.FontFamily, currentSize, baseFont.Style);
                    if (createdNew) fontToUse.Dispose();
                    fontToUse = nextFont;
                    createdNew = true;
                }
                g.DrawString(text, fontToUse, brush, rect, format);
            }
            finally
            {
                if (createdNew && fontToUse != null) fontToUse.Dispose();
            }
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
                code = code?.Trim() ?? "";
                if (string.IsNullOrEmpty(code)) return;

                // Set GDI+ options for crisp, aliased rendering (perfect for optical barcode scanners)
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

                var symbolIndices = new List<int>();

                // Check if the code is entirely numeric
                bool isAllDigits = true;
                foreach (char c in code)
                {
                    if (c < '0' || c > '9')
                    {
                        isAllDigits = false;
                        break;
                    }
                }

                if (isAllDigits && code.Length >= 2 && code.Length % 2 == 0)
                {
                    // Code 128 Subset C (Optimal 2-digits per symbol for numbers)
                    symbolIndices.Add(105); // Start C
                    int sum = 105;
                    int pos = 1;
                    for (int i = 0; i < code.Length; i += 2)
                    {
                        int val = int.Parse(code.Substring(i, 2));
                        symbolIndices.Add(val);
                        sum += val * pos;
                        pos++;
                    }
                    int checksum = sum % 103;
                    symbolIndices.Add(checksum);
                    symbolIndices.Add(106); // Stop
                }
                else if (isAllDigits && code.Length >= 3 && code.Length % 2 != 0)
                {
                    // Odd number of digits: Start C for digit pairs, switch to B for the last digit
                    symbolIndices.Add(105); // Start C
                    int sum = 105;
                    int pos = 1;
                    for (int i = 0; i < code.Length - 1; i += 2)
                    {
                        int val = int.Parse(code.Substring(i, 2));
                        symbolIndices.Add(val);
                        sum += val * pos;
                        pos++;
                    }

                    // Switch to Code B (Code B in Code C is 100)
                    symbolIndices.Add(100);
                    sum += 100 * pos;
                    pos++;

                    // Last character in Code B
                    int lastVal = code[code.Length - 1] - 32;
                    if (lastVal < 0 || lastVal > 95) lastVal = 0;
                    symbolIndices.Add(lastVal);
                    sum += lastVal * pos;
                    pos++;

                    int checksum = sum % 103;
                    symbolIndices.Add(checksum);
                    symbolIndices.Add(106); // Stop
                }
                else
                {
                    // Code 128 Subset B (Alphanumeric)
                    symbolIndices.Add(104); // Start B
                    int sum = 104;
                    for (int i = 0; i < code.Length; i++)
                    {
                        int val = code[i] - 32;
                        if (val < 0 || val > 95) val = 0; // fallback to Space
                        symbolIndices.Add(val);
                        sum += val * (i + 1);
                    }
                    int checksum = sum % 103;
                    symbolIndices.Add(checksum);
                    symbolIndices.Add(106); // Stop
                }

                int totalModules = 0;
                foreach (int index in symbolIndices)
                {
                    string pattern = Code128Patterns[index];
                    foreach (char c in pattern)
                    {
                        totalModules += (c - '0');
                    }
                }

                // Leave safe quiet zone margins
                float quietZoneModules = 10f; // 10 modules quiet zone each side
                float availableWidth = width;
                float moduleWidth = availableWidth / (totalModules + (quietZoneModules * 2f));
                
                // Allow readable module width, capped appropriately for sticker size
                float maxModuleWidth = (availableWidth < 130f) ? 1.5f : 2.0f; 
                if (moduleWidth > maxModuleWidth)
                {
                    moduleWidth = maxModuleWidth;
                }
                if (moduleWidth < 0.8f) moduleWidth = 0.8f;

                // Center the barcode with its quiet zones
                float actualBarcodeWidth = totalModules * moduleWidth;
                float curX = x + (width - actualBarcodeWidth) / 2f;

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
                                float barX = curX;
                                float barW = elementWidth;
                                g.FillRectangle(brush, barX, y, barW, height);
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

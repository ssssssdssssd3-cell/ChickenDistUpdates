using System;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Drawing.Printing;
using System.Windows.Forms;
using System.Text;
using ChickenDist.Core;
using ChickenDist.DAL;

namespace ChickenDist.Forms
{
    public class FrmPricePoster : Form
    {
        private CheckedListBox clbProducts;
        private PictureBox pbPreview;
        private Button btnRefresh;
        private Button btnCopy;
        private Button btnExportExcel;
        private Button btnExportPdf;
        private TextBox txtPosterTitle;
        private TextBox txtPosterNotes;
        private ComboBox cboPriceTier;
        private ComboBox cboBannerStyle;
        private ComboBox cboCategoryFilter;
        private TextBox txtSearchFilter;
        private CheckBox chkShowQty;
        private CheckBox chkShowPrice;
        private Bitmap _posterBitmap;
        private int _printItemIndex = 0;
        private int _pageNum = 1;

        public FrmPricePoster()
        {
            InitUI();
            LoadCategories();
            LoadProductsList();
            GeneratePoster();
        }

        private void InitUI()
        {
            this.Text = "لستة الأصناف (منشور الأسعار)";
            this.Size = new Size(1150, 750);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            // Table layout for left (controls) and right (preview)
            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                RightToLeft = RightToLeft.Yes
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 320f)); // Left: Controls
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));  // Right: Preview

            // Left panel (Controls)
            var pnlControls = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10), BackColor = Theme.BgCard, AutoScroll = true };
            
            int y = 10;
            var lblTitle = new Label { Text = "عنوان لستة الأصناف:", Location = new Point(10, y), AutoSize = true, ForeColor = Theme.TextMain, Font = Theme.FontBold };
            pnlControls.Controls.Add(lblTitle);
            y += 22;

            txtPosterTitle = new TextBox
            {
                Location = new Point(10, y),
                Width = 280,
                Text = "لستة الأصناف والأسعار",
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 11f)
            };
            pnlControls.Controls.Add(txtPosterTitle);
            y += 35;

            var lblPriceTier = new Label { Text = "فئة السعر المعروض:", Location = new Point(10, y), AutoSize = true, ForeColor = Theme.TextMain, Font = Theme.FontBold };
            pnlControls.Controls.Add(lblPriceTier);
            y += 22;

            cboPriceTier = new ComboBox
            {
                Location = new Point(10, y),
                Width = 280,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 10f)
            };
            cboPriceTier.Items.AddRange(new object[]
            {
                "سعر البيع قطاعي",
                "سعر البيع جملة",
                "سعر البيع نصف جملة",
                "سعر بيع قطعة (صغرى)",
                "سعر بيع علبة (وسطى)"
            });
            cboPriceTier.SelectedIndex = 0;
            cboPriceTier.SelectedIndexChanged += (s, e) => { LoadProductsList(); GeneratePoster(); };
            pnlControls.Controls.Add(cboPriceTier);
            y += 35;

            var lblBannerStyle = new Label { Text = "شكل وبانر الترويسة (PDF):", Location = new Point(10, y), AutoSize = true, ForeColor = Theme.TextMain, Font = Theme.FontBold };
            pnlControls.Controls.Add(lblBannerStyle);
            y += 22;

            cboBannerStyle = new ComboBox
            {
                Location = new Point(10, y),
                Width = 280,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 10f)
            };
            cboBannerStyle.Items.AddRange(new object[]
            {
                "رمادي كلاسيك (Classic Slate)",
                "أزرق ملكي (Royal Blue)",
                "أخضر هادئ (Forest Green)",
                "عنابي فاخر (Crimson Red)",
                "ذهبي داكن (Golden Amber)"
            });
            cboBannerStyle.SelectedIndex = 0;
            cboBannerStyle.SelectedIndexChanged += (s, e) => GeneratePoster();
            pnlControls.Controls.Add(cboBannerStyle);
            y += 35;

            // Category Filter
            var lblCategory = new Label { Text = "فلتر التصنيف:", Location = new Point(10, y), AutoSize = true, ForeColor = Theme.TextMain, Font = Theme.FontBold };
            pnlControls.Controls.Add(lblCategory);
            y += 22;

            cboCategoryFilter = new ComboBox
            {
                Location = new Point(10, y),
                Width = 280,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                Font = new Font("Segoe UI", 10f)
            };
            cboCategoryFilter.SelectedIndexChanged += (s, e) => { LoadProductsList(); GeneratePoster(); };
            pnlControls.Controls.Add(cboCategoryFilter);
            y += 35;

            // Search filter
            var lblSearch = new Label { Text = "بحث باسم الصنف:", Location = new Point(10, y), AutoSize = true, ForeColor = Theme.TextMain, Font = Theme.FontBold };
            pnlControls.Controls.Add(lblSearch);
            y += 22;

            txtSearchFilter = new TextBox
            {
                Location = new Point(10, y),
                Width = 280,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10f)
            };
            txtSearchFilter.TextChanged += (s, e) => { LoadProductsList(); GeneratePoster(); };
            pnlControls.Controls.Add(txtSearchFilter);
            y += 35;

            chkShowQty = new CheckBox
            {
                Text = "إظهار عمود الكمية (الرصيد المتاح)",
                Location = new Point(10, y),
                Width = 280,
                Height = 25,
                ForeColor = Theme.TextMain,
                Font = Theme.FontBold,
                Checked = false
            };
            chkShowQty.CheckedChanged += (s, e) => GeneratePoster();
            pnlControls.Controls.Add(chkShowQty);
            y += 30;

            chkShowPrice = new CheckBox
            {
                Text = "إظهار عمود السعر",
                Location = new Point(10, y),
                Width = 280,
                Height = 25,
                ForeColor = Theme.TextMain,
                Font = Theme.FontBold,
                Checked = true
            };
            chkShowPrice.CheckedChanged += (s, e) => GeneratePoster();
            pnlControls.Controls.Add(chkShowPrice);
            y += 30;

            var lblNotes = new Label { Text = "ملاحظات في الترويسة:", Location = new Point(10, y), AutoSize = true, ForeColor = Theme.TextMain, Font = Theme.FontBold };
            pnlControls.Controls.Add(lblNotes);
            y += 22;

            txtPosterNotes = new TextBox
            {
                Location = new Point(10, y),
                Width = 280,
                Height = 60,
                Multiline = true,
                Text = "الأسعار سارية حتى نفاذ الكمية أو تحديث آخر.",
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9.5f)
            };
            pnlControls.Controls.Add(txtPosterNotes);
            y += 70;

            var lblProducts = new Label { Text = "اختر الأصناف لتضمينها:", Location = new Point(10, y), AutoSize = true, ForeColor = Theme.TextMain, Font = Theme.FontBold };
            pnlControls.Controls.Add(lblProducts);
            y += 22;

            clbProducts = new CheckedListBox
            {
                Location = new Point(10, y),
                Width = 280,
                Height = 220,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle,
                CheckOnClick = true,
                Font = new Font("Segoe UI", 10f)
            };
            pnlControls.Controls.Add(clbProducts);
            y += 230;

            btnRefresh = Theme.MakeButton("🔄 تحديث المعاينة", 10, y, 280, 36, Theme.Primary);
            btnRefresh.Click += (s, e) => GeneratePoster();
            pnlControls.Controls.Add(btnRefresh);
            y += 42;

            btnCopy = Theme.MakeButton("📋 نسخ الصورة للحافظة", 10, y, 280, 38, Theme.Success);
            btnCopy.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            btnCopy.Click += BtnCopy_Click;
            pnlControls.Controls.Add(btnCopy);
            y += 44;

            btnExportExcel = Theme.MakeButton("📊 تصدير إكسيل (Excel)", 10, y, 280, 38, Color.FromArgb(30, 96, 64));
            btnExportExcel.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            btnExportExcel.Click += BtnExportExcel_Click;
            pnlControls.Controls.Add(btnExportExcel);
            y += 44;

            btnExportPdf = Theme.MakeButton("🖨️ طباعة وتصدير PDF", 10, y, 280, 38, Color.FromArgb(70, 80, 95));
            btnExportPdf.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            btnExportPdf.Click += BtnExportPdf_Click;
            pnlControls.Controls.Add(btnExportPdf);
            y += 44;

            // Right panel (Preview)
            var pnlPreview = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Theme.BgMain, Padding = new Padding(15) };
            
            pbPreview = new PictureBox
            {
                SizeMode = PictureBoxSizeMode.AutoSize,
                Location = new Point(0, 0),
                BackColor = Color.White
            };
            pnlPreview.Controls.Add(pbPreview);

            tbl.Controls.Add(pnlControls, 0, 0);
            tbl.Controls.Add(pnlPreview, 1, 0);
            this.Controls.Add(tbl);

            Theme.ApplyFormRTL(this);
            this.RightToLeftLayout = false;
        }

        private void LoadCategories()
        {
            try
            {
                cboCategoryFilter.Items.Clear();
                cboCategoryFilter.Items.Add(new CategoryItem { ID = 0, Name = "-- كل التصنيفات --" });
                var dt = DbHelper.Query("SELECT CategoryID, CategoryName FROM Categories WHERE IsActive=1 ORDER BY CategoryName");
                foreach (DataRow r in dt.Rows)
                {
                    cboCategoryFilter.Items.Add(new CategoryItem
                    {
                        ID = Convert.ToInt32(r["CategoryID"]),
                        Name = r["CategoryName"].ToString()
                    });
                }
                cboCategoryFilter.SelectedIndex = 0;
            }
            catch { cboCategoryFilter.Items.Add(new CategoryItem { ID = 0, Name = "-- كل التصنيفات --" }); cboCategoryFilter.SelectedIndex = 0; }
        }

        private void LoadProductsList()
        {
            try
            {
                clbProducts.Items.Clear();
                string priceField = "SalePrice"; // default
                int tierIdx = cboPriceTier.SelectedIndex;
                if (tierIdx == 1) priceField = "WholesalePrice";
                else if (tierIdx == 2) priceField = "SemiWholesalePrice";
                else if (tierIdx == 3) priceField = "Unit1SalePrice";
                else if (tierIdx == 4) priceField = "Unit2SalePrice";

                string unitSelect = (tierIdx == 3) ? "COALESCE(Unit1Name, N'قطعة')" 
                                  : (tierIdx == 4) ? "COALESCE(Unit2Name, N'علبة')" 
                                  : "COALESCE(Unit, N'قطعة')";

                // Build WHERE clause
                string whereExtra = "";
                int selectedCatID = (cboCategoryFilter.SelectedItem as CategoryItem)?.ID ?? 0;
                if (selectedCatID > 0)
                    whereExtra += $" AND p.CategoryID = {selectedCatID}";

                string searchTxt = txtSearchFilter?.Text?.Trim() ?? "";
                if (!string.IsNullOrEmpty(searchTxt))
                    whereExtra += $" AND (p.ProductName LIKE N'%{searchTxt.Replace("'", "''")}%' OR p.ProductCode LIKE N'%{searchTxt.Replace("'", "''")}%')";

                string sql = $@"
                    SELECT 
                        p.ProductID, 
                        p.ProductCode, 
                        p.ProductName, 
                        COALESCE(p.Unit1Name, N'قطعة') AS Unit1Name,
                        COALESCE(p.Unit2Name, N'علبة') AS Unit2Name,
                        COALESCE(p.Unit, N'قطعة') AS Unit3Name,
                        COALESCE(p.Unit2Factor, 1.0) AS Unit2Factor,
                        COALESCE(p.Unit3Factor, 1.0) AS Unit3Factor,
                        COALESCE(p.{priceField}, 0) AS PriceVal, 
                        {unitSelect} AS UnitName,
                        ISNULL(s.StockQty, 0) AS StockQty
                    FROM Products p
                    LEFT JOIN (
                        SELECT ProductID, SUM(CurrentQty) AS StockQty 
                        FROM vw_CurrentStockByWarehouse 
                        GROUP BY ProductID
                    ) s ON p.ProductID = s.ProductID
                    WHERE p.IsActive = 1{whereExtra}
                    ORDER BY p.ProductName";

                var dt = DbHelper.Query(sql);
                foreach (DataRow r in dt.Rows)
                {
                    decimal stockQty = Convert.ToDecimal(r["StockQty"]);
                    decimal unit2Factor = r["Unit2Factor"] != DBNull.Value ? Convert.ToDecimal(r["Unit2Factor"]) : 1m;
                    decimal unit3Factor = r["Unit3Factor"] != DBNull.Value ? Convert.ToDecimal(r["Unit3Factor"]) : 1m;
                    
                    decimal convertedQty = 0m;
                    if (tierIdx == 3)
                    {
                        convertedQty = stockQty;
                    }
                    else if (tierIdx == 4)
                    {
                        decimal div = (unit2Factor > 0m) ? unit2Factor : 1m;
                        convertedQty = stockQty / div;
                    }
                    else
                    {
                        decimal div = (unit3Factor * unit2Factor > 0m) ? (unit3Factor * unit2Factor) : (unit3Factor > 0m ? unit3Factor : 1m);
                        convertedQty = stockQty / div;
                    }

                    clbProducts.Items.Add(new ProductItem
                    {
                        ID = Convert.ToInt32(r["ProductID"]),
                        Code = r["ProductCode"]?.ToString() ?? "",
                        Name = r["ProductName"].ToString(),
                        Price = Convert.ToDecimal(r["PriceVal"]),
                        Unit = r["UnitName"].ToString(),
                        Qty = convertedQty
                    }, true); // check by default
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("فشل تحميل قائمة الأصناف:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void GeneratePoster()
        {
            int selectedCount = clbProducts.CheckedItems.Count;
            if (selectedCount == 0)
            {
                pbPreview.Image = null;
                return;
            }

            int totalW = 750;
            int headerH = 160;
            int notesH = string.IsNullOrWhiteSpace(txtPosterNotes.Text) ? 0 : 50;
            int tableHeaderH = 35;
            int footerH = 60;

            bool showQty   = chkShowQty   != null && chkShowQty.Checked;
            bool showPrice = chkShowPrice == null || chkShowPrice.Checked;
            int colSerialW = 40;
            int colSerialX = totalW - 40 - colSerialW;
            int colNameX, colUnitX, colQtyX, colPriceX;
            int colNameW, colUnitW, colQtyW, colPriceW;

            if (showQty && showPrice)
            {
                colNameX = 290; colNameW = 380;
                colUnitX = 200; colUnitW = 90;
                colQtyX  = 120; colQtyW  = 80;
                colPriceX = 40; colPriceW = 80;
            }
            else if (showQty && !showPrice)
            {
                colNameX = 230; colNameW = 440;
                colUnitX = 130; colUnitW = 100;
                colQtyX  = 40;  colQtyW  = 90;
                colPriceX = 0;  colPriceW = 0;
            }
            else if (!showQty && showPrice)
            {
                colNameX = 260; colNameW = 410;
                colUnitX = 140; colUnitW = 120;
                colQtyX  = 0;   colQtyW  = 0;
                colPriceX = 40; colPriceW = 100;
            }
            else
            {
                colNameX = 40;  colNameW = 630;
                colUnitX = 0;   colUnitW = 0;
                colQtyX  = 0;   colQtyW  = 0;
                colPriceX = 0;  colPriceW = 0;
            }

            // Pre-calculate dynamic row height for each item (allowing long names to wrap cleanly onto 2 lines)
            var rowHeights = new System.Collections.Generic.List<int>();
            using (var bmpMeasure = new Bitmap(1, 1))
            using (var gMeasure = Graphics.FromImage(bmpMeasure))
            using (var fBoldMeasure = new Font("Arial", 10.5f, FontStyle.Bold))
            {
                var rtlMeasure = new StringFormat { Alignment = StringAlignment.Near, FormatFlags = StringFormatFlags.DirectionRightToLeft };
                foreach (ProductItem item in clbProducts.CheckedItems)
                {
                    SizeF sz = gMeasure.MeasureString(item.Name, fBoldMeasure, colNameW - 14, rtlMeasure);
                    int rh = (sz.Height > 20f || sz.Width > colNameW - 16f) ? 48 : 30;
                    rowHeights.Add(rh);
                }
            }

            int rowsTotalH = 0;
            foreach (int h in rowHeights) rowsTotalH += h;
            int totalH = headerH + notesH + tableHeaderH + rowsTotalH + footerH + 40;

            if (_posterBitmap != null) _posterBitmap.Dispose();
            _posterBitmap = new Bitmap(totalW, totalH);

            using (var g = Graphics.FromImage(_posterBitmap))
            {
                g.Clear(Color.White);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                // Thin elegant border around the page
                using (var penBorder = new Pen(Color.FromArgb(200, 200, 200), 1))
                {
                    g.DrawRectangle(penBorder, 10, 10, totalW - 20, totalH - 20);
                }

                // Fonts
                var fTitle = new Font("Arial", 18f, FontStyle.Bold);
                var fComp = new Font("Arial", 13f, FontStyle.Bold);
                var fSub = new Font("Arial", 9.5f);
                var fBold = new Font("Arial", 10.5f, FontStyle.Bold);
                var fRegular = new Font("Arial", 10f);

                var center = new StringFormat { Alignment = StringAlignment.Center };
                var rtlNear = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.DirectionRightToLeft };
                var rtlCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.DirectionRightToLeft };

                float yCur = 25;

                // Draw Header Banner Box
                int bannerStyle = cboBannerStyle != null ? cboBannerStyle.SelectedIndex : 0;
                Color bannerBgColor = Color.FromArgb(30, 41, 59); // default classic slate
                Color bannerTextColor = Color.White;

                if (bannerStyle == 1) bannerBgColor = Color.FromArgb(30, 58, 138); // Royal Blue
                else if (bannerStyle == 2) bannerBgColor = Color.FromArgb(6, 78, 59); // Forest Green
                else if (bannerStyle == 3) bannerBgColor = Color.FromArgb(127, 29, 29); // Crimson Red
                else if (bannerStyle == 4) bannerBgColor = Color.FromArgb(120, 53, 15); // Golden Amber

                // Draw banner rectangle
                var bannerRect = new RectangleF(40, yCur, totalW - 80, 110);
                using (var brushBanner = new SolidBrush(bannerBgColor))
                {
                    g.FillRectangle(brushBanner, bannerRect);
                }

                // Draw logo inside the banner if enabled
                if (AppConfig.PrintShopLogo && !string.IsNullOrEmpty(AppConfig.ShopLogoPath) && System.IO.File.Exists(AppConfig.ShopLogoPath))
                {
                    try
                    {
                        using (var img = Image.FromFile(AppConfig.ShopLogoPath))
                        {
                            g.DrawImage(img, 50, yCur + 15, 80, 80);
                        }
                    }
                    catch {}
                }

                // Draw Text inside the banner
                using (var brushBannerText = new SolidBrush(bannerTextColor))
                {
                    float textY = yCur + 15;
                    g.DrawString(AppConfig.CompanyName, fComp, brushBannerText, new RectangleF(0, textY, totalW, 25), center);
                    textY += 25;

                    g.DrawString(txtPosterTitle.Text, fTitle, brushBannerText, new RectangleF(0, textY, totalW, 35), center);
                    textY += 35;

                    string infoStr = $"التاريخ: {DateTime.Today:dd/MM/yyyy}";
                    string phoneStr = "";
                    if (!string.IsNullOrWhiteSpace(AppConfig.CompanyPhone1)) phoneStr += AppConfig.CompanyPhone1;
                    if (!string.IsNullOrWhiteSpace(AppConfig.CompanyPhone2))
                    {
                        if (phoneStr != "") phoneStr += " - ";
                        phoneStr += AppConfig.CompanyPhone2;
                    }
                    if (phoneStr != "") infoStr += $"  |  📞 هاتف: {phoneStr}";

                    g.DrawString(infoStr, fSub, brushBannerText, new RectangleF(0, textY, totalW, 20), center);
                }

                yCur += 120;

                // Notes Box (if any)
                if (notesH > 0)
                {
                    var notesRect = new RectangleF(40, yCur, totalW - 80, notesH - 10);
                    using (var brushBox = new SolidBrush(Color.FromArgb(249, 250, 251)))
                    using (var penBox = new Pen(Color.FromArgb(229, 231, 235), 1))
                    {
                        g.FillRectangle(brushBox, notesRect);
                        g.DrawRectangle(penBox, notesRect.X, notesRect.Y, notesRect.Width, notesRect.Height);
                    }
                    g.DrawString("ملاحظات: " + txtPosterNotes.Text, fRegular, Brushes.DarkRed, new RectangleF(notesRect.X + 10, notesRect.Y + 8, notesRect.Width - 20, notesRect.Height - 16), rtlNear);
                    yCur += notesH;
                }

                // Draw Table Header
                using (var brushHeader = new SolidBrush(Color.FromArgb(30, 41, 59)))
                {
                    g.FillRectangle(brushHeader, 40, yCur, totalW - 80, tableHeaderH);
                }

                using (var brushWhite = new SolidBrush(Color.White))
                {
                    g.DrawString("م", fBold, brushWhite, new RectangleF(colSerialX, yCur, colSerialW, tableHeaderH), rtlCenter);
                    g.DrawString("اسم الصنف", fBold, brushWhite, new RectangleF(colNameX + 6, yCur, colNameW - 12, tableHeaderH), rtlNear);
                    if (colUnitW > 0) g.DrawString("الوحدة", fBold, brushWhite, new RectangleF(colUnitX, yCur, colUnitW, tableHeaderH), rtlCenter);
                    if (showQty && colQtyW > 0)
                        g.DrawString("الكمية", fBold, brushWhite, new RectangleF(colQtyX, yCur, colQtyW, tableHeaderH), rtlCenter);
                    if (showPrice && colPriceW > 0)
                        g.DrawString("السعر", fBold, brushWhite, new RectangleF(colPriceX, yCur, colPriceW, tableHeaderH), rtlCenter);
                }

                using (var penGrid = new Pen(Color.FromArgb(229, 231, 235), 1))
                {
                    g.DrawRectangle(penGrid, 40, yCur, totalW - 80, tableHeaderH);
                }

                yCur += tableHeaderH;

                // Draw Table Rows
                int idx = 1;
                using (var penGrid = new Pen(Color.FromArgb(229, 231, 235), 1))
                using (var brushBlack = new SolidBrush(Color.Black))
                using (var brushLightRow = new SolidBrush(Color.FromArgb(245, 245, 245)))
                using (var brushDarkRow = new SolidBrush(Color.FromArgb(225, 225, 225)))
                {
                    for (int i = 0; i < clbProducts.CheckedItems.Count; i++)
                    {
                        var item = (ProductItem)clbProducts.CheckedItems[i];
                        int thisRowH = (i < rowHeights.Count) ? rowHeights[i] : 30;

                        if (idx % 2 == 0)
                        {
                            g.FillRectangle(brushDarkRow, 40, yCur, totalW - 80, thisRowH);
                        }
                        else
                        {
                            g.FillRectangle(brushLightRow, 40, yCur, totalW - 80, thisRowH);
                        }

                        g.DrawString(idx.ToString(), fRegular, brushBlack, new RectangleF(colSerialX, yCur, colSerialW, thisRowH), rtlCenter);
                        // Name cell - wraps onto 2 lines cleanly if long
                        g.DrawString(item.Name, fBold, brushBlack, new RectangleF(colNameX + 6, yCur + 4, colNameW - 12, thisRowH - 8), rtlNear);
                        
                        if (colUnitW > 0)
                        {
                            string unitStr = string.IsNullOrWhiteSpace(item.Unit) ? "قطعة" : item.Unit;
                            g.DrawString(unitStr, fRegular, brushBlack, new RectangleF(colUnitX, yCur, colUnitW, thisRowH), rtlCenter);
                        }
                        if (showQty && colQtyW > 0)
                            g.DrawString(item.Qty.ToString("N0"), fBold, brushBlack, new RectangleF(colQtyX, yCur, colQtyW, thisRowH), rtlCenter);
                        if (showPrice && colPriceW > 0)
                            g.DrawString(item.Price.ToString("N2") + " ج", fBold, Brushes.Crimson, new RectangleF(colPriceX, yCur, colPriceW, thisRowH), rtlCenter);

                        // Draw Grid Lines
                        g.DrawLine(penGrid, 40, yCur + thisRowH, totalW - 40, yCur + thisRowH);
                        g.DrawLine(penGrid, colSerialX, yCur, colSerialX, yCur + thisRowH);
                        if (colNameX > 40) g.DrawLine(penGrid, colNameX, yCur, colNameX, yCur + thisRowH);
                        if (colUnitW > 0) g.DrawLine(penGrid, colUnitX, yCur, colUnitX, yCur + thisRowH);
                        if (showQty && colQtyW > 0) g.DrawLine(penGrid, colQtyX, yCur, colQtyX, yCur + thisRowH);
                        g.DrawLine(penGrid, 40, yCur, 40, yCur + thisRowH);
                        g.DrawLine(penGrid, totalW - 40, yCur, totalW - 40, yCur + thisRowH);

                        yCur += thisRowH;
                        idx++;
                    }
                }

                // Draw Footer
                yCur += 15;
                g.DrawLine(new Pen(Color.FromArgb(200, 200, 200), 1), 40, yCur, totalW - 40, yCur);
                yCur += 10;
                var fFooter = new Font("Arial", 11f, FontStyle.Bold | FontStyle.Italic);
                g.DrawString("نشرف بخدمتكم دائماً  |  شكراً لتعاملكم معنا", fFooter, Brushes.DimGray, new RectangleF(0, yCur, totalW, 25), center);
            }

            pbPreview.Image = _posterBitmap;
        }

        private void BtnCopy_Click(object sender, EventArgs e)
        {
            if (_posterBitmap == null) return;
            try
            {
                Clipboard.SetImage(_posterBitmap);
                MessageBox.Show("✅ تم نسخ ملصق الأسعار للحافظة بنجاح!\nيمكنك الآن لصقه مباشرة (Ctrl+V) في الواتساب أو الفيس بوك لإرساله للعملاء.",
                                "تم النسخ", MessageBoxButtons.OK, MessageBoxIcon.Information,
                                MessageBoxDefaultButton.Button1,
                                MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);
            }
            catch (Exception ex)
            {
                MessageBox.Show("فشل نسخ الصورة للحافظة:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnExportExcel_Click(object sender, EventArgs e)
        {
            if (clbProducts.CheckedItems.Count == 0)
            {
                MessageBox.Show("يرجى اختيار أصناف للتصدير أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = "Excel Files (*.xls)|*.xls|HTML Files (*.html)|*.html";
                sfd.FileName = $"{txtPosterTitle.Text}_{DateTime.Today:yyyy-MM-dd}.xls";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        bool showQty   = chkShowQty   != null && chkShowQty.Checked;
                        bool showPrice = chkShowPrice == null || chkShowPrice.Checked;
                        int colCount = 3 + (showQty ? 1 : 0) + (showPrice ? 1 : 0);

                        var sb = new StringBuilder();
                        sb.AppendLine("<html xmlns:o=\"urn:schemas-microsoft-com:office:office\" xmlns:x=\"urn:schemas-microsoft-com:office:excel\" xmlns=\"http://www.w3.org/TR/REC-html40\">");
                        sb.AppendLine("<head>");
                        sb.AppendLine("<meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\">");
                        sb.AppendLine("<style>");
                        sb.AppendLine("  body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; direction: rtl; }");
                        sb.AppendLine("  table { border-collapse: collapse; width: 100%; border: 2px solid #1e293b; }");
                        sb.AppendLine("  th { background-color: #1e293b; color: #ffffff; border: 1px solid #cbd5e1; padding: 12px; font-weight: bold; text-align: center; font-size: 11pt; }");
                        sb.AppendLine("  td { border: 1px solid #cbd5e1; padding: 10px; text-align: center; font-size: 10pt; }");
                        sb.AppendLine("  .title-row { font-size: 16pt; font-weight: bold; color: #ffffff; background-color: #1e3a8a; text-align: center; padding: 15px; }");
                        sb.AppendLine("  .info-row { font-size: 10pt; color: #ffffff; background-color: #3b82f6; text-align: center; padding: 8px; }");
                        sb.AppendLine("  .notes-row { font-size: 10pt; color: #7f1d1d; background-color: #fee2e2; text-align: right; padding: 10px; font-weight: bold; }");
                        sb.AppendLine("  .row-light { background-color: #f8fafc; }");
                        sb.AppendLine("  .row-dark { background-color: #e2e8f0; }");
                        sb.AppendLine("  .price-cell { font-weight: bold; color: #b91c1c; font-size: 11pt; }");
                        sb.AppendLine("  .name-cell { text-align: right; padding-right: 15px; font-weight: bold; }");
                        sb.AppendLine("</style>");
                        sb.AppendLine("</head>");
                        sb.AppendLine("<body>");
                        sb.AppendLine("<table>");
                        
                        // Header rows
                        sb.AppendLine($"  <tr><td colspan=\"{colCount}\" class=\"title-row\">{AppConfig.CompanyName} - {txtPosterTitle.Text}</td></tr>");
                        
                        string phoneStr = "";
                        if (!string.IsNullOrWhiteSpace(AppConfig.CompanyPhone1)) phoneStr += AppConfig.CompanyPhone1;
                        if (!string.IsNullOrWhiteSpace(AppConfig.CompanyPhone2))
                        {
                            if (phoneStr != "") phoneStr += " - ";
                            phoneStr += AppConfig.CompanyPhone2;
                        }
                        string infoText = $"التاريخ: {DateTime.Today:dd/MM/yyyy}";
                        if (phoneStr != "") infoText += $" | هاتف: {phoneStr}";
                        sb.AppendLine($"  <tr><td colspan=\"{colCount}\" class=\"info-row\">{infoText}</td></tr>");
                        
                        if (!string.IsNullOrWhiteSpace(txtPosterNotes.Text))
                        {
                            sb.AppendLine($"  <tr><td colspan=\"{colCount}\" class=\"notes-row\">ملاحظات: {txtPosterNotes.Text}</td></tr>");
                        }

                        // Table Headers
                        sb.AppendLine("  <tr>");
                        sb.AppendLine("    <th style=\"width: 6%;\">م</th>");
                        sb.AppendLine("    <th style=\"width: 14%;\">كود الصنف</th>");
                        sb.AppendLine("    <th style=\"width: 44%;\">اسم الصنف</th>");
                        sb.AppendLine("    <th style=\"width: 12%;\">الوحدة</th>");
                        if (showQty)  sb.AppendLine("    <th style=\"width: 12%;\">الكمية</th>");
                        if (showPrice) sb.AppendLine("    <th style=\"width: 12%;\">السعر</th>");
                        sb.AppendLine("  </tr>");

                        // Rows
                        int idx = 1;
                        foreach (ProductItem item in clbProducts.CheckedItems)
                        {
                            string rowClass = (idx % 2 == 0) ? "row-dark" : "row-light";
                            sb.AppendLine($"  <tr class=\"{rowClass}\">");
                            sb.AppendLine($"    <td>{idx}</td>");
                            sb.AppendLine($"    <td>{item.Code}</td>");
                            sb.AppendLine($"    <td class=\"name-cell\">{item.Name}</td>");
                            sb.AppendLine($"    <td>{(string.IsNullOrWhiteSpace(item.Unit) ? "قطعة" : item.Unit)}</td>");
                            if (showQty)   sb.AppendLine($"    <td>{item.Qty:N0}</td>");
                            if (showPrice) sb.AppendLine($"    <td class=\"price-cell\">{item.Price:N2} ج</td>");
                            sb.AppendLine("  </tr>");
                            idx++;
                        }
                        
                        sb.AppendLine("</table>");
                        sb.AppendLine("</body>");
                        sb.AppendLine("</html>");

                        System.IO.File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
                        MessageBox.Show("✅ تم تصدير الملف بنجاح!", "تم التصدير", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("فشل تصدير الملف:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void BtnExportPdf_Click(object sender, EventArgs e)
        {
            if (clbProducts.CheckedItems.Count == 0)
            {
                MessageBox.Show("يرجى اختيار أصناف أولاً للطباعة.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            _printItemIndex = 0;
            _pageNum = 1;
            var pd = new PrintDocument();
            pd.PrintController = new StandardPrintController();
            pd.PrintPage += PrintDoc_PrintPage;
            using (var dlg = new PrintDialog())
            {
                dlg.Document = pd;
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    AppConfig.PrintInBackground(pd);
                }
            }
        }

        private void PrintDoc_PrintPage(object sender, PrintPageEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            float xStart = e.MarginBounds.Left;
            float yStart = e.MarginBounds.Top;
            float pageWidth = e.MarginBounds.Width;
            float pageHeight = e.MarginBounds.Height;
            float yCur = yStart;

            // Draw border
            using (var penBorder = new Pen(Color.FromArgb(200, 200, 200), 1))
            {
                g.DrawRectangle(penBorder, xStart, yStart, pageWidth, pageHeight);
            }

            // Fonts
            var fTitle = new Font("Arial", 16f, FontStyle.Bold);
            var fComp = new Font("Arial", 12f, FontStyle.Bold);
            var fSub = new Font("Arial", 9f);
            var fBold = new Font("Arial", 10f, FontStyle.Bold);
            var fRegular = new Font("Arial", 9.5f);

            var center = new StringFormat { Alignment = StringAlignment.Center };
            var rtlNear = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.DirectionRightToLeft };
            var rtlCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.DirectionRightToLeft };

            bool showQty   = chkShowQty   != null && chkShowQty.Checked;
            bool showPrice = chkShowPrice == null || chkShowPrice.Checked;

            // Page 1 Header
            if (_pageNum == 1)
            {
                int bannerStyle = cboBannerStyle != null ? cboBannerStyle.SelectedIndex : 0;
                Color bannerBgColor = Color.FromArgb(30, 41, 59); // default classic slate

                if (bannerStyle == 1) bannerBgColor = Color.FromArgb(30, 58, 138); // Royal Blue
                else if (bannerStyle == 2) bannerBgColor = Color.FromArgb(6, 78, 59); // Forest Green
                else if (bannerStyle == 3) bannerBgColor = Color.FromArgb(127, 29, 29); // Crimson Red
                else if (bannerStyle == 4) bannerBgColor = Color.FromArgb(120, 53, 15); // Golden Amber

                // Draw banner
                var bannerRect = new RectangleF(xStart + 20, yCur + 10, pageWidth - 40, 90);
                using (var brushBanner = new SolidBrush(bannerBgColor))
                {
                    g.FillRectangle(brushBanner, bannerRect);
                }

                // Draw Logo if enabled
                if (AppConfig.PrintShopLogo && !string.IsNullOrEmpty(AppConfig.ShopLogoPath) && System.IO.File.Exists(AppConfig.ShopLogoPath))
                {
                    try
                    {
                        using (var img = Image.FromFile(AppConfig.ShopLogoPath))
                        {
                            g.DrawImage(img, xStart + 30, yCur + 20, 70, 70);
                        }
                    }
                    catch {}
                }

                // Draw Banner Text
                using (var brushWhite = new SolidBrush(Color.White))
                {
                    float textY = yCur + 20;
                    g.DrawString(AppConfig.CompanyName, fComp, brushWhite, new RectangleF(xStart, textY, pageWidth, 20), center);
                    textY += 20;
                    g.DrawString(txtPosterTitle.Text, fTitle, brushWhite, new RectangleF(xStart, textY, pageWidth, 30), center);
                    textY += 28;

                    string infoStr = $"التاريخ: {DateTime.Today:dd/MM/yyyy}";
                    string phoneStr = "";
                    if (!string.IsNullOrWhiteSpace(AppConfig.CompanyPhone1)) phoneStr += AppConfig.CompanyPhone1;
                    if (!string.IsNullOrWhiteSpace(AppConfig.CompanyPhone2))
                    {
                        if (phoneStr != "") phoneStr += " - ";
                        phoneStr += AppConfig.CompanyPhone2;
                    }
                    if (phoneStr != "") infoStr += $"  |  📞 هاتف: {phoneStr}";

                    g.DrawString(infoStr, fSub, brushWhite, new RectangleF(xStart, textY, pageWidth, 18), center);
                }

                yCur += 110;

                // Notes Box
                if (!string.IsNullOrWhiteSpace(txtPosterNotes.Text))
                {
                    var notesRect = new RectangleF(xStart + 20, yCur, pageWidth - 40, 35);
                    using (var brushBox = new SolidBrush(Color.FromArgb(249, 250, 251)))
                    using (var penBox = new Pen(Color.FromArgb(229, 231, 235), 1))
                    {
                        g.FillRectangle(brushBox, notesRect);
                        g.DrawRectangle(penBox, notesRect.X, notesRect.Y, notesRect.Width, notesRect.Height);
                    }
                    g.DrawString("ملاحظات: " + txtPosterNotes.Text, fRegular, Brushes.DarkRed, new RectangleF(notesRect.X + 10, notesRect.Y + 8, notesRect.Width - 20, notesRect.Height - 16), rtlNear);
                    yCur += 45;
                }
            }

            // Columns sizing based on pageWidth
            float colSerialW  = 45;
            float colPriceW   = (showPrice) ? 70 : 0;
            float colQtyW     = (showQty)   ? 75 : 0;
            float colUnitW    = 85;
            float colNameW    = pageWidth - colSerialW - colUnitW - colQtyW - colPriceW;

            float colSerialX  = xStart + pageWidth - colSerialW;
            float colNameX    = colSerialX - colNameW;
            float colUnitX    = colNameX - colUnitW;
            float colQtyX     = showQty   ? (colUnitX - colQtyW)   : 0;
            float colPriceX   = xStart; // far left

            float headerH = 28;
            float rowH = 24;

            // Draw Table Header
            using (var brushHeader = new SolidBrush(Color.FromArgb(30, 41, 59)))
            {
                g.FillRectangle(brushHeader, xStart, yCur, pageWidth, headerH);
            }
            using (var brushWhite = new SolidBrush(Color.White))
            {
                g.DrawString("م", fBold, brushWhite, new RectangleF(colSerialX, yCur, colSerialW, headerH), rtlCenter);
                g.DrawString("اسم الصنف", fBold, brushWhite, new RectangleF(colNameX + 5, yCur, colNameW - 10, headerH), rtlNear);
                g.DrawString("الوحدة", fBold, brushWhite, new RectangleF(colUnitX, yCur, colUnitW, headerH), rtlCenter);
                if (showQty && colQtyW > 0)  g.DrawString("الكمية", fBold, brushWhite, new RectangleF(colQtyX, yCur, colQtyW, headerH), rtlCenter);
                if (showPrice && colPriceW > 0) g.DrawString("السعر", fBold, brushWhite, new RectangleF(colPriceX, yCur, colPriceW, headerH), rtlCenter);
            }
            yCur += headerH;

            // Print rows
            int itemsCount = clbProducts.CheckedItems.Count;
            using (var penGrid = new Pen(Color.FromArgb(229, 231, 235), 1))
            using (var brushBlack = new SolidBrush(Color.Black))
            using (var brushLightRow = new SolidBrush(Color.FromArgb(249, 250, 251)))
            using (var brushDarkRow = new SolidBrush(Color.FromArgb(241, 245, 249)))
            {
                while (_printItemIndex < itemsCount)
                {
                    var item = (ProductItem)clbProducts.CheckedItems[_printItemIndex];

                    // Check if item name wraps onto 2 lines
                    SizeF sz = g.MeasureString(item.Name, fBold, (int)(colNameW - 12), rtlNear);
                    float thisRowH = (sz.Height > 18f || sz.Width > colNameW - 14f) ? 38f : 24f;

                    // Check page boundary. Leave 50 units for page footer
                    if (yCur + thisRowH + 50 > yStart + pageHeight)
                    {
                        // Draw page number in footer before leaving
                        g.DrawString($"صفحة {_pageNum}", fSub, Brushes.DimGray, new RectangleF(xStart, yStart + pageHeight - 25, pageWidth, 20), center);
                        
                        _pageNum++;
                        e.HasMorePages = true;
                        return;
                    }

                    // Draw alternating row background
                    var rowRect = new RectangleF(xStart, yCur, pageWidth, thisRowH);
                    g.FillRectangle((_printItemIndex % 2 == 0) ? brushLightRow : brushDarkRow, rowRect);

                    // Draw row texts
                    g.DrawString((_printItemIndex + 1).ToString(), fRegular, brushBlack, new RectangleF(colSerialX, yCur, colSerialW, thisRowH), rtlCenter);
                    // Name cell - wraps onto 2 lines cleanly
                    g.DrawString(item.Name, fBold, brushBlack, new RectangleF(colNameX + 5, yCur + 3, colNameW - 10, thisRowH - 6), rtlNear);
                    
                    string unitStr = string.IsNullOrWhiteSpace(item.Unit) ? "قطعة" : item.Unit;
                    g.DrawString(unitStr, fRegular, brushBlack, new RectangleF(colUnitX, yCur, colUnitW, thisRowH), rtlCenter);
                    
                    if (showQty && colQtyW > 0)
                        g.DrawString(item.Qty.ToString("N0"), fBold, brushBlack, new RectangleF(colQtyX, yCur, colQtyW, thisRowH), rtlCenter);
                    if (showPrice && colPriceW > 0)
                        g.DrawString(item.Price.ToString("N2") + " ج", fBold, Brushes.Crimson, new RectangleF(colPriceX, yCur, colPriceW, thisRowH), rtlCenter);

                    // Draw Grid Lines
                    g.DrawLine(penGrid, xStart, yCur + thisRowH, xStart + pageWidth, yCur + thisRowH);
                    g.DrawLine(penGrid, colSerialX, yCur, colSerialX, yCur + thisRowH);
                    g.DrawLine(penGrid, colNameX, yCur, colNameX, yCur + thisRowH);
                    g.DrawLine(penGrid, colUnitX, yCur, colUnitX, yCur + thisRowH);
                    if (showQty && colQtyW > 0) g.DrawLine(penGrid, colQtyX, yCur, colQtyX, yCur + thisRowH);

                    yCur += thisRowH;
                    _printItemIndex++;
                }
            }

            // Draw final footer
            yCur += 10;
            if (yCur + 40 <= yStart + pageHeight)
            {
                g.DrawLine(new Pen(Color.FromArgb(200, 200, 200), 1), xStart + 20, yCur, xStart + pageWidth - 40, yCur);
                yCur += 8;
                var fFooter = new Font("Arial", 10f, FontStyle.Bold | FontStyle.Italic);
                g.DrawString("نشرف بخدمتكم دائماً  |  شكراً لتعاملكم معنا", fFooter, Brushes.DimGray, new RectangleF(xStart, yCur, pageWidth, 20), center);
            }

            // Draw page number on last page
            g.DrawString($"صفحة {_pageNum}", fSub, Brushes.DimGray, new RectangleF(xStart, yStart + pageHeight - 25, pageWidth, 20), center);

            // Reset print state
            e.HasMorePages = false;
            _printItemIndex = 0;
            _pageNum = 1;
        }

        private class ProductItem
        {
            public int ID { get; set; }
            public string Code { get; set; }
            public string Name { get; set; }
            public decimal Price { get; set; }
            public string Unit { get; set; }
            public decimal Qty { get; set; }

            public override string ToString()
            {
                return $"{Name} ({Price:0.##} ج)";
            }
        }

        private class CategoryItem
        {
            public int ID { get; set; }
            public string Name { get; set; }
            public override string ToString() => Name;
        }
    }
}

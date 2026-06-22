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
        private Bitmap _posterBitmap;

        public FrmPricePoster()
        {
            InitUI();
            LoadProductsList();
            GeneratePoster();
        }

        private void InitUI()
        {
            this.Text = "منشور الأسعار اليومية";
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
            var lblTitle = new Label { Text = "عنوان المنشور:", Location = new Point(10, y), AutoSize = true, ForeColor = Theme.TextMain, Font = Theme.FontBold };
            pnlControls.Controls.Add(lblTitle);
            y += 22;

            txtPosterTitle = new TextBox
            {
                Location = new Point(10, y),
                Width = 280,
                Text = "قائمة أسعار اليوم",
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

                string sql = $"SELECT ProductID, ProductName, COALESCE({priceField}, 0) AS PriceVal, {unitSelect} AS UnitName " +
                             "FROM Products WHERE IsActive = 1 ORDER BY ProductName";

                var dt = DbHelper.Query(sql);
                foreach (DataRow r in dt.Rows)
                {
                    clbProducts.Items.Add(new ProductItem
                    {
                        ID = Convert.ToInt32(r["ProductID"]),
                        Name = r["ProductName"].ToString(),
                        Price = Convert.ToDecimal(r["PriceVal"]),
                        Unit = r["UnitName"].ToString()
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
            int headerH = 150;
            int notesH = string.IsNullOrWhiteSpace(txtPosterNotes.Text) ? 0 : 50;
            int tableHeaderH = 35;
            int rowH = 30;
            int footerH = 60;
            int totalH = headerH + notesH + tableHeaderH + (selectedCount * rowH) + footerH + 40;

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
                var rtlNear = new StringFormat { Alignment = StringAlignment.Near, FormatFlags = StringFormatFlags.DirectionRightToLeft };
                var rtlCenter = new StringFormat { Alignment = StringAlignment.Center, FormatFlags = StringFormatFlags.DirectionRightToLeft };

                float yCur = 25;

                // Draw logo if enabled
                if (AppConfig.PrintShopLogo && !string.IsNullOrEmpty(AppConfig.ShopLogoPath) && System.IO.File.Exists(AppConfig.ShopLogoPath))
                {
                    try
                    {
                        using (var img = Image.FromFile(AppConfig.ShopLogoPath))
                        {
                            g.DrawImage(img, 40, yCur, 80, 80);
                        }
                    }
                    catch {}
                }

                // Draw Header Text
                using (var brushText = new SolidBrush(Color.FromArgb(17, 24, 39)))
                {
                    g.DrawString(AppConfig.CompanyName, fComp, brushText, new RectangleF(0, yCur, totalW, 25), center);
                    yCur += 25;

                    g.DrawString(txtPosterTitle.Text, fTitle, brushText, new RectangleF(0, yCur, totalW, 35), center);
                    yCur += 35;

                    g.DrawString($"التاريخ: {DateTime.Today:dd/MM/yyyy}", fSub, Brushes.Gray, new RectangleF(0, yCur, totalW, 20), center);
                    yCur += 20;

                    string phoneStr = "";
                    if (!string.IsNullOrWhiteSpace(AppConfig.CompanyPhone1)) phoneStr += AppConfig.CompanyPhone1;
                    if (!string.IsNullOrWhiteSpace(AppConfig.CompanyPhone2))
                    {
                        if (phoneStr != "") phoneStr += " - ";
                        phoneStr += AppConfig.CompanyPhone2;
                    }
                    if (phoneStr != "")
                    {
                        g.DrawString($"📞 هاتف: {phoneStr}", fSub, Brushes.DimGray, new RectangleF(0, yCur, totalW, 20), center);
                        yCur += 20;
                    }
                }

                yCur += 10;

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

                // Table Layout coordinates
                int colSerialX = totalW - 40 - 40; // 670
                int colNameX = 260;
                int colUnitX = 140;
                int colPriceX = 40;

                // Draw Table Header
                using (var brushHeader = new SolidBrush(Color.FromArgb(30, 41, 59)))
                {
                    g.FillRectangle(brushHeader, 40, yCur, totalW - 80, tableHeaderH);
                }

                using (var brushWhite = new SolidBrush(Color.White))
                {
                    g.DrawString("م", fBold, brushWhite, new RectangleF(colSerialX, yCur + 8, 40, tableHeaderH), rtlCenter);
                    g.DrawString("اسم الصنف", fBold, brushWhite, new RectangleF(colNameX, yCur + 8, 410, tableHeaderH), rtlNear);
                    g.DrawString("الوحدة", fBold, brushWhite, new RectangleF(colUnitX, yCur + 8, 120, tableHeaderH), rtlCenter);
                    g.DrawString("السعر", fBold, brushWhite, new RectangleF(colPriceX, yCur + 8, 100, tableHeaderH), rtlCenter);
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
                using (var brushAltRow = new SolidBrush(Color.FromArgb(249, 250, 251)))
                {
                    foreach (ProductItem item in clbProducts.CheckedItems)
                    {
                        if (idx % 2 == 0)
                        {
                            g.FillRectangle(brushAltRow, 40, yCur, totalW - 80, rowH);
                        }

                        g.DrawString(idx.ToString(), fRegular, brushBlack, new RectangleF(colSerialX, yCur + 6, 40, rowH), rtlCenter);
                        g.DrawString(item.Name, fBold, brushBlack, new RectangleF(colNameX, yCur + 6, 410, rowH), rtlNear);
                        
                        string unitStr = string.IsNullOrWhiteSpace(item.Unit) ? "قطعة" : item.Unit;
                        g.DrawString(unitStr, fRegular, brushBlack, new RectangleF(colUnitX, yCur + 6, 120, rowH), rtlCenter);
                        
                        g.DrawString(item.Price.ToString("N2") + " ج", fBold, Brushes.Crimson, new RectangleF(colPriceX, yCur + 6, 100, rowH), rtlCenter);

                        // Draw Grid Lines
                        g.DrawLine(penGrid, 40, yCur + rowH, totalW - 40, yCur + rowH);
                        g.DrawLine(penGrid, colSerialX, yCur, colSerialX, yCur + rowH);
                        g.DrawLine(penGrid, colNameX, yCur, colNameX, yCur + rowH);
                        g.DrawLine(penGrid, colUnitX, yCur, colUnitX, yCur + rowH);
                        g.DrawLine(penGrid, 40, yCur, 40, yCur + rowH);
                        g.DrawLine(penGrid, totalW - 40, yCur, totalW - 40, yCur + rowH);

                        yCur += rowH;
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
                sfd.Filter = "CSV Files (*.csv)|*.csv";
                sfd.FileName = $"{txtPosterTitle.Text}_{DateTime.Today:yyyy-MM-dd}.csv";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var sb = new StringBuilder();
                        sb.Append('\uFEFF'); // BOM for UTF-8 Excel support
                        sb.AppendLine("م,اسم الصنف,الوحدة,السعر");
                        int idx = 1;
                        foreach (ProductItem item in clbProducts.CheckedItems)
                        {
                            string name = item.Name.Contains(",") ? $"\"{item.Name}\"" : item.Name;
                            string unit = item.Unit.Contains(",") ? $"\"{item.Unit}\"" : item.Unit;
                            sb.AppendLine($"{idx},{name},{unit},{item.Price:0.##}");
                            idx++;
                        }

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
            if (_posterBitmap == null) return;
            using (var pd = new PrintDocument())
            {
                pd.PrintPage += PrintDoc_PrintPage;
                using (var dlg = new PrintDialog())
                {
                    dlg.Document = pd;
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        pd.Print();
                    }
                }
            }
        }

        private void PrintDoc_PrintPage(object sender, PrintPageEventArgs e)
        {
            if (_posterBitmap == null) return;
            Rectangle marginBounds = e.MarginBounds;
            float scale = Math.Min((float)marginBounds.Width / _posterBitmap.Width, (float)marginBounds.Height / _posterBitmap.Height);
            int w = (int)(_posterBitmap.Width * scale);
            int h = (int)(_posterBitmap.Height * scale);
            int x = marginBounds.Left + (marginBounds.Width - w) / 2;
            int y = marginBounds.Top + (marginBounds.Height - h) / 2;
            e.Graphics.DrawImage(_posterBitmap, x, y, w, h);
            e.HasMorePages = false;
        }

        private class ProductItem
        {
            public int ID { get; set; }
            public string Name { get; set; }
            public decimal Price { get; set; }
            public string Unit { get; set; }

            public override string ToString()
            {
                return $"{Name} ({Price:0.##} ج)";
            }
        }
    }
}

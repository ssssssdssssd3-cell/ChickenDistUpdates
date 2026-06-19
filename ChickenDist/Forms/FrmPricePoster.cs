using System;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
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
        private TextBox txtPosterTitle;
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
            this.Size = new Size(950, 720);
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
            var pnlControls = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10), BackColor = Theme.BgCard };
            
            var lblTitle = new Label { Text = "عنوان المنشور:", Location = new Point(10, 10), AutoSize = true, ForeColor = Theme.TextMain, Font = Theme.FontBold };
            pnlControls.Controls.Add(lblTitle);

            txtPosterTitle = new TextBox
            {
                Location = new Point(10, 32),
                Width = 280,
                Text = "قائمة أسعار اليوم",
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 11f)
            };
            pnlControls.Controls.Add(txtPosterTitle);

            var lblProducts = new Label { Text = "اختر الأصناف لتضمينها:", Location = new Point(10, 75), AutoSize = true, ForeColor = Theme.TextMain, Font = Theme.FontBold };
            pnlControls.Controls.Add(lblProducts);

            clbProducts = new CheckedListBox
            {
                Location = new Point(10, 97),
                Width = 280,
                Height = 380,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextMain,
                BorderStyle = BorderStyle.FixedSingle,
                CheckOnClick = true,
                Font = new Font("Segoe UI", 10f)
            };
            pnlControls.Controls.Add(clbProducts);

            btnRefresh = Theme.MakeButton("🔄 تحديث الملصق", 10, 490, 280, 38, Theme.Primary);
            btnRefresh.Click += (s, e) => GeneratePoster();
            pnlControls.Controls.Add(btnRefresh);

            btnCopy = Theme.MakeButton("📋 نسخ الصورة للحافظة", 10, 538, 280, 44, Theme.Success);
            btnCopy.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            btnCopy.Click += BtnCopy_Click;
            pnlControls.Controls.Add(btnCopy);

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
        }

        private void LoadProductsList()
        {
            try
            {
                clbProducts.Items.Clear();
                var dt = DbHelper.Query("SELECT ProductID, ProductName, SalePrice AS RetailPrice, Unit FROM Products WHERE IsActive = 1 ORDER BY ProductName");
                foreach (DataRow r in dt.Rows)
                {
                    clbProducts.Items.Add(new ProductItem
                    {
                        ID = Convert.ToInt32(r["ProductID"]),
                        Name = r["ProductName"].ToString(),
                        Price = Convert.ToDecimal(r["RetailPrice"]),
                        Unit = r["Unit"].ToString()
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

            // Calculate height dynamically
            int headerH = 120;
            int footerH = 60;
            int cardW = 265;
            int cardH = 75;
            int gap = 12;
            int colsCount = 2;

            int rowsCount = (selectedCount + 1) / colsCount;
            int totalH = headerH + (rowsCount * (cardH + gap)) + footerH + 30;
            int totalW = 600;

            if (_posterBitmap != null) _posterBitmap.Dispose();
            _posterBitmap = new Bitmap(totalW, totalH);

            using (var g = Graphics.FromImage(_posterBitmap))
            {
                g.Clear(Color.FromArgb(17, 24, 39)); // Modern Dark slate color (Tailwind Slate-900)
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                // Border
                var cBorder = Color.FromArgb(59, 130, 246); // Royal Blue
                using (var penBorder = new Pen(cBorder, 4))
                {
                    g.DrawRectangle(penBorder, 8, 8, totalW - 16, totalH - 16);
                }

                // Decorative header glow/banner
                using (var brushBanner = new LinearGradientBrush(new Rectangle(12, 12, totalW - 24, headerH - 10), Color.FromArgb(30, 58, 138), Color.FromArgb(17, 24, 39), LinearGradientMode.Vertical))
                {
                    g.FillRectangle(brushBanner, 12, 12, totalW - 24, headerH - 10);
                }

                // Fonts
                var fTitle = new Font("Arial", 22f, FontStyle.Bold);
                var fComp = new Font("Arial", 13f, FontStyle.Bold);
                var fDate = new Font("Arial", 9.5f);
                var fProdName = new Font("Arial", 11f, FontStyle.Bold);
                var fProdUnit = new Font("Arial", 8.5f);
                var fProdPrice = new Font("Arial", 13f, FontStyle.Bold);

                var center = new StringFormat { Alignment = StringAlignment.Center };
                var rtlNear = new StringFormat { Alignment = StringAlignment.Near, FormatFlags = StringFormatFlags.DirectionRightToLeft };
                var rtlCenter = new StringFormat { Alignment = StringAlignment.Center, FormatFlags = StringFormatFlags.DirectionRightToLeft };

                // Draw Header Text
                using (var bWhite = new SolidBrush(Color.White))
                using (var bGold = new SolidBrush(Color.FromArgb(245, 158, 11))) // Amber/Gold color
                {
                    g.DrawString(txtPosterTitle.Text, fTitle, bGold, new RectangleF(0, 22, totalW, 35), center);
                    g.DrawString(AppConfig.CompanyName, fComp, bWhite, new RectangleF(0, 62, totalW, 25), center);
                    g.DrawString($"التاريخ: {DateTime.Today:dd/MM/yyyy}", fDate, Brushes.LightGray, new RectangleF(0, 88, totalW, 20), center);
                }

                // Draw Products
                float startX = 25;
                float startY = headerH + 10;

                int i = 0;
                foreach (ProductItem item in clbProducts.CheckedItems)
                {
                    int col = i % colsCount;
                    int row = i / colsCount;

                    float x = startX + col * (cardW + gap + 10);
                    float y = startY + row * (cardH + gap);

                    // Card background
                    using (var brushCard = new SolidBrush(Color.FromArgb(31, 41, 55))) // Tailwind Slate-800
                    using (var penCardBorder = new Pen(Color.FromArgb(75, 85, 99), 1.5f)) // Border Slate-600
                    {
                        // Draw card
                        g.FillRectangle(brushCard, x, y, cardW, cardH);
                        g.DrawRectangle(penCardBorder, x, y, cardW, cardH);
                    }

                    // Product Name
                    g.DrawString(item.Name, fProdName, Brushes.White, new RectangleF(x + 85, y + 14, cardW - 95, 25), rtlNear);
                    // Product Unit
                    string unitStr = string.IsNullOrWhiteSpace(item.Unit) ? "قطعة" : item.Unit;
                    g.DrawString($"الوحدة: {unitStr}", fProdUnit, Brushes.LightGray, new RectangleF(x + 85, y + 42, cardW - 95, 20), rtlNear);

                    // Product Price (drawn in an accent box on the left)
                    using (var brushPriceBox = new SolidBrush(Color.FromArgb(239, 68, 68))) // Bright Red
                    {
                        g.FillRectangle(brushPriceBox, x + 10, y + 15, 70, 45);
                    }
                    g.DrawString($"{item.Price:0.##}", fProdPrice, Brushes.White, new RectangleF(x + 10, y + 20, 70, 25), rtlCenter);
                    g.DrawString("جنيه", fProdUnit, Brushes.White, new RectangleF(x + 10, y + 43, 70, 15), rtlCenter);

                    i++;
                }

                // Draw Footer
                g.DrawLine(new Pen(Color.FromArgb(55, 65, 81), 1.5f), 20, totalH - footerH, totalW - 20, totalH - footerH);
                using (var bWhite = new SolidBrush(Color.White))
                {
                    g.DrawString("🙏 شكراً لتعاملكم معنا  |  نتشرف بخدمتكم دائماً", fComp, bWhite, new RectangleF(0, totalH - footerH + 18, totalW, 25), center);
                }
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

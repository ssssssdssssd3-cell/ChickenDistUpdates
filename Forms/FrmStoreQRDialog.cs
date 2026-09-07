using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.IO;
using System.Windows.Forms;
using ChickenDist.Core;
using QRCoder;

namespace ChickenDist.Forms
{
    /// <summary>
    /// نافذة عرض، طباعة، ومشاركة رمز الاستجابة السريع (QR Code) الخاص بالمتجر الإلكتروني للعملاء
    /// تدعم: طباعة بوستر إعلاني للمحل (A4)، طباعة إيصال كاشير حراري (80mm)،
    /// حفظ الصورة (PNG)، نسخ الصورة للحافظة، ومشاركة الرابط عبر واتساب.
    /// </summary>
    public class FrmStoreQRDialog : Form
    {
        private PictureBox picQR;
        private TextBox txtUrl;
        private Button btnCopyUrl;
        private Button btnOpenBrowser;
        private Button btnPrintPoster;
        private Button btnPrintThermal;
        private Button btnSaveImage;
        private Button btnCopyImage;
        private Button btnShareWhatsApp;
        private Button btnClose;

        private Bitmap _qrBitmap;
        private string _storeUrl;
        private string _companyName;
        private string _storePhone;
        private string _announcement;

        public FrmStoreQRDialog()
        {
            InitializeData();
            InitializeComponent();
            GenerateQrBitmap();
        }

        private void InitializeData()
        {
            string projectId = AppConfig.Get("FirebaseProjectId", "checkin-192ab");
            if (string.IsNullOrEmpty(projectId)) projectId = "checkin-192ab";
            _storeUrl = $"https://{projectId}.web.app/store.html";

            _companyName = !string.IsNullOrWhiteSpace(AppConfig.CompanyName) ? AppConfig.CompanyName : "المتجر الإلكتروني";
            _storePhone = !string.IsNullOrWhiteSpace(AppConfig.Store_OrderNotificationWhatsApp)
                ? AppConfig.Store_OrderNotificationWhatsApp
                : AppConfig.CompanyPhone;
            _announcement = AppConfig.Store_Announcement ?? "أهلاً بكم في متجرنا الإلكتروني! خدمة التوصيل متوفرة.";
        }

        private void InitializeComponent()
        {
            this.Text = "📱 رمز المتجر الإلكتروني (Store QR Code) | طباعة ومشاركة";
            this.Size = new Size(760, 680);
            this.MinimumSize = new Size(700, 620);
            this.StartPosition = FormStartPosition.CenterParent;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Color.FromArgb(15, 23, 42);
            this.Font = new Font("Segoe UI", 9.5f);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // 1. الشريط العلوي (Header)
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Color.FromArgb(15, 23, 42),
                Padding = new Padding(16, 10, 16, 8)
            };

            var lblTitle = new Label
            {
                Text = "📱 رمز الاستجابة السريع للمتجر (Store QR Code)",
                Font = new Font("Segoe UI", 12.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(56, 189, 248),
                Dock = DockStyle.Top,
                Height = 26,
                TextAlign = ContentAlignment.MiddleRight
            };

            var lblSubtitle = new Label
            {
                Text = "اطبع الرمز كبوستر للمحل أو إيصال حراري، أو احفظه وشاركه مع عملائك عبر واتساب وشبكات التواصل",
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = Color.FromArgb(148, 163, 184),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight
            };

            pnlHeader.Controls.Add(lblSubtitle);
            pnlHeader.Controls.Add(lblTitle);

            // 2. الشريط السفلي (Footer)
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 55,
                BackColor = Color.FromArgb(15, 23, 42),
                Padding = new Padding(16, 8, 16, 10)
            };

            btnClose = new Button
            {
                Text = "إغلاق النافذة",
                Size = new Size(130, 36),
                BackColor = Color.FromArgb(51, 65, 85),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Dock = DockStyle.Left
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => this.Close();

            pnlFooter.Controls.Add(btnClose);

            // 3. المحتوى الأوسط (Main Container: QR Card + Actions)
            var pnlMain = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16, 4, 16, 4),
                BackColor = Color.FromArgb(15, 23, 42)
            };

            // بطاقة عرض الكود (QR Card Container)
            var pnlCard = new Panel
            {
                Location = new Point(16, 4),
                Size = new Size(710, 360),
                BackColor = Color.FromArgb(24, 33, 53),
                Padding = new Padding(16)
            };

            var lblStoreName = new Label
            {
                Text = "🏪 " + _companyName,
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(248, 250, 252),
                Location = new Point(16, 12),
                Size = new Size(678, 30),
                TextAlign = ContentAlignment.MiddleCenter
            };

            picQR = new PictureBox
            {
                Location = new Point(235, 46),
                Size = new Size(240, 240),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Cursor = Cursors.Hand
            };
            picQR.Click += (s, e) => CopyImageToClipboard();

            // شريط رابط المتجر والنسخ
            var pnlUrlBar = new Panel
            {
                Location = new Point(16, 298),
                Size = new Size(678, 38),
                BackColor = Color.FromArgb(15, 23, 42),
                Padding = new Padding(4)
            };

            btnOpenBrowser = new Button
            {
                Text = "🌐 فتح",
                Size = new Size(75, 30),
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Dock = DockStyle.Left
            };
            btnOpenBrowser.FlatAppearance.BorderSize = 0;
            btnOpenBrowser.Click += (s, e) => OpenInBrowser();

            btnCopyUrl = new Button
            {
                Text = "📋 نسخ الرابط",
                Size = new Size(110, 30),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Dock = DockStyle.Left
            };
            btnCopyUrl.FlatAppearance.BorderSize = 0;
            btnCopyUrl.Click += (s, e) => CopyUrl();

            txtUrl = new TextBox
            {
                Text = _storeUrl,
                ReadOnly = true,
                BackColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.FromArgb(56, 189, 248),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                BorderStyle = BorderStyle.None,
                Dock = DockStyle.Fill
            };

            pnlUrlBar.Controls.Add(txtUrl);
            pnlUrlBar.Controls.Add(btnCopyUrl);
            pnlUrlBar.Controls.Add(btnOpenBrowser);

            pnlCard.Controls.Add(pnlUrlBar);
            pnlCard.Controls.Add(picQR);
            pnlCard.Controls.Add(lblStoreName);

            // 4. أزرار العمليات والطباعة (Actions Grid)
            var pnlActions = new TableLayoutPanel
            {
                Location = new Point(16, 372),
                Size = new Size(710, 165),
                ColumnCount = 3,
                RowCount = 2,
                BackColor = Color.FromArgb(15, 23, 42),
                Padding = new Padding(0)
            };
            pnlActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            pnlActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            pnlActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            pnlActions.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            pnlActions.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

            btnPrintPoster = CreateActionButton("🖨️ طباعة بوستر للمحل (A4)", Color.FromArgb(37, 99, 235), (s, e) => PrintA4Poster());
            btnPrintThermal = CreateActionButton("🧾 طباعة إيصال حراري (80mm)", Color.FromArgb(13, 148, 136), (s, e) => PrintThermalReceipt());
            btnShareWhatsApp = CreateActionButton("💬 مشاركة عبر واتساب", Color.FromArgb(34, 197, 94), (s, e) => ShareViaWhatsApp());

            btnCopyImage = CreateActionButton("📋 نسخ صورة الرمز (للحافظة)", Color.FromArgb(147, 51, 234), (s, e) => CopyImageToClipboard());
            btnSaveImage = CreateActionButton("💾 حفظ الرمز كصورة (PNG)", Color.FromArgb(217, 119, 6), (s, e) => SaveImageAsPng());
            var btnDirectWeb = CreateActionButton("🚀 تجربة المتجر في المتصفح", Color.FromArgb(71, 85, 105), (s, e) => OpenInBrowser());

            pnlActions.Controls.Add(btnPrintPoster, 0, 0);
            pnlActions.Controls.Add(btnPrintThermal, 1, 0);
            pnlActions.Controls.Add(btnShareWhatsApp, 2, 0);

            pnlActions.Controls.Add(btnCopyImage, 0, 1);
            pnlActions.Controls.Add(btnSaveImage, 1, 1);
            pnlActions.Controls.Add(btnDirectWeb, 2, 1);

            pnlMain.Controls.Add(pnlActions);
            pnlMain.Controls.Add(pnlCard);

            this.Controls.Add(pnlMain);
            this.Controls.Add(pnlFooter);
            this.Controls.Add(pnlHeader);
        }

        private Button CreateActionButton(string text, Color bg, EventHandler onClick)
        {
            var btn = new Button
            {
                Text = text,
                Dock = DockStyle.Fill,
                BackColor = bg,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(4)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += onClick;
            return btn;
        }

        private void GenerateQrBitmap()
        {
            try
            {
                using (var qrGen = new QRCodeGenerator())
                {
                    var qrData = qrGen.CreateQrCode(_storeUrl, QRCodeGenerator.ECCLevel.H);
                    using (var qrCode = new QRCode(qrData))
                    {
                        // 15 pixels per module creates a crisp high-res 600x600+ image
                        _qrBitmap = qrCode.GetGraphic(15, Color.Black, Color.White, true);
                        picQR.Image = _qrBitmap;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في توليد رمز الـ QR: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void CopyUrl()
        {
            try
            {
                Clipboard.SetText(_storeUrl);
                MessageBox.Show("تم نسخ رابط المتجر الإلكتروني للحافظة بنجاح ✅", "تم النسخ", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("تعذر النسخ: " + ex.Message, "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OpenInBrowser()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = _storeUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("تعذر فتح الرابط في المتصفح: " + ex.Message, "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void CopyImageToClipboard()
        {
            if (_qrBitmap == null) return;
            try
            {
                Clipboard.SetImage(_qrBitmap);
                MessageBox.Show("تم نسخ صورة رمز الـ QR للحافظة بنجاح! 📋\nيمكنك الآن لصقها في واتساب ويب أو أي برنامج آخر (Ctrl + V).", "تم نسخ الصورة", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("تعذر نسخ الصورة: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void SaveImageAsPng()
        {
            if (_qrBitmap == null) return;
            try
            {
                using (var sfd = new SaveFileDialog())
                {
                    sfd.Title = "حفظ كود QR المتجر الإلكتروني كصورة";
                    sfd.Filter = "ملف صورة PNG (*.png)|*.png|صورة JPEG (*.jpg)|*.jpg";
                    sfd.FileName = "Store_QR_" + DateTime.Now.ToString("yyyyMMdd") + ".png";

                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        _qrBitmap.Save(sfd.FileName, System.Drawing.Imaging.ImageFormat.Png);
                        MessageBox.Show("تم حفظ صورة الـ QR بنجاح في:\n" + sfd.FileName, "تم الحفظ بنجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("حدث خطأ أثناء حفظ الصورة: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShareViaWhatsApp()
        {
            try
            {
                string msg = $"مرحباً بكم في متجرنا الإلكتروني ({_companyName})! 🛒✨\n\n" +
                             "يمكنكم الآن تصفح كافة منتجاتنا والأسعار والطلب أونلاين بكل سهولة وسرعة عبر الرابط التالي:\n" +
                             $"{_storeUrl}\n\n" +
                             (!string.IsNullOrEmpty(_announcement) ? $"📢 {_announcement}\n\n" : "") +
                             "خدمة التوصيل والمتابعة متوفرة 🚀";

                string encoded = Uri.EscapeDataString(msg);
                string waUrl = $"https://api.whatsapp.com/send?text={encoded}";

                Process.Start(new ProcessStartInfo
                {
                    FileName = waUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("تعذر فتح واتساب: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// طباعة بوستر تسويقي أنيق للمحل بحجم A4
        /// </summary>
        private void PrintA4Poster()
        {
            if (_qrBitmap == null) return;

            try
            {
                var pd = new PrintDocument();
                pd.DocumentName = "Store_QR_Poster_" + _companyName;
                pd.DefaultPageSettings.PaperSize = new PaperSize("A4", 827, 1169); // Standard A4 (100 DPI)
                pd.DefaultPageSettings.Margins = new Margins(40, 40, 40, 40);

                if (!string.IsNullOrEmpty(AppConfig.A4PrinterName))
                {
                    AppConfig.SetPrinter(pd, AppConfig.A4PrinterName);
                }

                pd.PrintPage += (s, ev) =>
                {
                    Graphics g = ev.Graphics;
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                    int pageWidth = ev.PageBounds.Width;
                    int pageHeight = ev.PageBounds.Height;

                    // 1. الإطار الخارجي والزخرفة
                    using (var borderPen = new Pen(Color.FromArgb(203, 213, 225), 3))
                    {
                        g.DrawRectangle(borderPen, 35, 35, pageWidth - 70, pageHeight - 70);
                    }
                    using (var innerPen = new Pen(Color.FromArgb(37, 99, 235), 1.5f))
                    {
                        g.DrawRectangle(innerPen, 42, 42, pageWidth - 84, pageHeight - 84);
                    }

                    // 2. ترويسة البوستر (Header Card)
                    using (var headerBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))
                    {
                        g.FillRectangle(headerBrush, 45, 45, pageWidth - 90, 150);
                    }

                    // اسم المحل / الشركة
                    using (var fontCompany = new Font("Segoe UI", 26f, FontStyle.Bold))
                    using (var brushWhite = new SolidBrush(Color.White))
                    {
                        var sfCenter = new StringFormat
                        {
                            Alignment = StringAlignment.Center,
                            LineAlignment = StringAlignment.Center,
                            FormatFlags = StringFormatFlags.DirectionRightToLeft
                        };
                        g.DrawString(_companyName, fontCompany, brushWhite, new RectangleF(45, 55, pageWidth - 90, 65), sfCenter);
                    }

                    // العنوان الفرعي الترويجي
                    using (var fontSub = new Font("Segoe UI", 15f, FontStyle.Bold))
                    using (var brushSky = new SolidBrush(Color.FromArgb(56, 189, 248)))
                    {
                        var sfCenter = new StringFormat
                        {
                            Alignment = StringAlignment.Center,
                            LineAlignment = StringAlignment.Center,
                            FormatFlags = StringFormatFlags.DirectionRightToLeft
                        };
                        g.DrawString("🛒 اطلب أونلاين بكل سهولة وسرعة من متجرنا الإلكتروني", fontSub, brushSky, new RectangleF(45, 125, pageWidth - 90, 45), sfCenter);
                    }

                    // 3. كود الـ QR الكبير في المنتصف
                    int qrSize = 390;
                    int qrX = (pageWidth - qrSize) / 2;
                    int qrY = 240;

                    // خلفية وإطار كود الـ QR
                    using (var cardBrush = new SolidBrush(Color.White))
                    using (var cardPen = new Pen(Color.FromArgb(226, 232, 240), 2))
                    {
                        g.FillRectangle(cardBrush, qrX - 20, qrY - 20, qrSize + 40, qrSize + 40);
                        g.DrawRectangle(cardPen, qrX - 20, qrY - 20, qrSize + 40, qrSize + 40);
                    }

                    g.DrawImage(_qrBitmap, qrX, qrY, qrSize, qrSize);

                    // 4. تعليمات المسح للعملاء
                    int textY = qrY + qrSize + 45;
                    using (var fontGuide = new Font("Segoe UI", 18f, FontStyle.Bold))
                    using (var brushDark = new SolidBrush(Color.FromArgb(15, 23, 42)))
                    {
                        var sf = new StringFormat
                        {
                            Alignment = StringAlignment.Center,
                            LineAlignment = StringAlignment.Center,
                            FormatFlags = StringFormatFlags.DirectionRightToLeft
                        };
                        g.DrawString("📱 وجّه كاميرا هاتفك نحو الرمز للتصفح والطلب الفوري", fontGuide, brushDark, new RectangleF(50, textY, pageWidth - 100, 45), sf);
                    }

                    // 5. رابط المتجر النصي
                    using (var fontUrl = new Font("Segoe UI", 13f, FontStyle.Bold))
                    using (var brushBlue = new SolidBrush(Color.FromArgb(37, 99, 235)))
                    {
                        var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                        g.DrawString(_storeUrl, fontUrl, brushBlue, new RectangleF(50, textY + 50, pageWidth - 100, 30), sf);
                    }

                    // 6. صندوق أرقام التواصل والطلب
                    int infoY = textY + 95;
                    using (var infoBoxBrush = new SolidBrush(Color.FromArgb(241, 245, 249)))
                    using (var infoBoxPen = new Pen(Color.FromArgb(203, 213, 225), 1))
                    {
                        g.FillRectangle(infoBoxBrush, 80, infoY, pageWidth - 160, 110);
                        g.DrawRectangle(infoBoxPen, 80, infoY, pageWidth - 160, 110);
                    }

                    using (var fontPhone = new Font("Segoe UI", 15f, FontStyle.Bold))
                    using (var brushPhone = new SolidBrush(Color.FromArgb(15, 23, 42)))
                    {
                        var sf = new StringFormat
                        {
                            Alignment = StringAlignment.Center,
                            LineAlignment = StringAlignment.Center,
                            FormatFlags = StringFormatFlags.DirectionRightToLeft
                        };
                        string phoneText = !string.IsNullOrEmpty(_storePhone) ? $"📞 للطلب والاستفسار أو الدعم عبر واتساب: {_storePhone}" : "🚀 خدمة التوصيل متوفرة لجميع المناطق";
                        g.DrawString(phoneText, fontPhone, brushPhone, new RectangleF(80, infoY + 12, pageWidth - 160, 40), sf);
                    }

                    using (var fontAnnounce = new Font("Segoe UI", 11.5f, FontStyle.Regular))
                    using (var brushAnnounce = new SolidBrush(Color.FromArgb(71, 85, 105)))
                    {
                        var sf = new StringFormat
                        {
                            Alignment = StringAlignment.Center,
                            LineAlignment = StringAlignment.Center,
                            FormatFlags = StringFormatFlags.DirectionRightToLeft
                        };
                        g.DrawString(_announcement, fontAnnounce, brushAnnounce, new RectangleF(80, infoY + 55, pageWidth - 160, 45), sf);
                    }

                    // 7. تذييل البوستر (Footer)
                    using (var fontFooter = new Font("Segoe UI", 9.5f, FontStyle.Regular))
                    using (var brushMuted = new SolidBrush(Color.FromArgb(148, 163, 184)))
                    {
                        var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                        g.DrawString("Powered by ProSoft ERP - Smart Business Systems", fontFooter, brushMuted, new RectangleF(50, pageHeight - 65, pageWidth - 100, 20), sf);
                    }

                    ev.HasMorePages = false;
                };

                using (var ppd = new PrintPreviewDialog())
                {
                    ppd.Document = pd;
                    ppd.Width = 950;
                    ppd.Height = 780;
                    ppd.StartPosition = FormStartPosition.CenterParent;
                    ppd.Text = "معاينة طباعة بوستر المتجر الإلكتروني (A4)";
                    ppd.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("حدث خطأ أثناء إعداد طباعة البوستر: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// طباعة إيصال حراري سريع (80mm / 58mm) على طابعة الكاشير
        /// </summary>
        private void PrintThermalReceipt()
        {
            if (_qrBitmap == null) return;

            try
            {
                var pd = new PrintDocument();
                pd.DocumentName = "Store_QR_Receipt";
                pd.PrintController = new StandardPrintController(); // Suppress popup

                int paperW = AppConfig.ReceiptPaperWidth == 58 ? 205 : 300;
                pd.DefaultPageSettings.PaperSize = new PaperSize("Receipt", paperW, 800);
                pd.DefaultPageSettings.Margins = new Margins(6, 6, 8, 8);

                if (!string.IsNullOrEmpty(AppConfig.ReceiptPrinterName))
                {
                    AppConfig.SetPrinter(pd, AppConfig.ReceiptPrinterName);
                }

                pd.PrintPage += (s, ev) =>
                {
                    Graphics g = ev.Graphics;
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;

                    int printableWidth = paperW;
                    int y = 10;

                    var sfCenter = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center,
                        FormatFlags = StringFormatFlags.DirectionRightToLeft
                    };

                    // 1. اسم المحل
                    using (var fontTitle = new Font("Segoe UI", 12f, FontStyle.Bold))
                    using (var brush = new SolidBrush(Color.Black))
                    {
                        g.DrawString(_companyName, fontTitle, brush, new RectangleF(0, y, printableWidth, 30), sfCenter);
                    }
                    y += 32;

                    // 2. نوع الإيصال
                    using (var fontSub = new Font("Segoe UI", 9.5f, FontStyle.Bold))
                    using (var brush = new SolidBrush(Color.Black))
                    {
                        g.DrawString("📱 متجرنا الإلكتروني أونلاين", fontSub, brush, new RectangleF(0, y, printableWidth, 22), sfCenter);
                    }
                    y += 24;

                    // خط فاصل
                    using (var pen = new Pen(Color.Black, 1) { DashStyle = DashStyle.Dash })
                    {
                        g.DrawLine(pen, 10, y, printableWidth - 10, y);
                    }
                    y += 8;

                    // 3. كود الـ QR الحراري (حجم مناسب لطابعة الفواتير 180x180)
                    int thermalQrSize = paperW == 205 ? 150 : 180;
                    int qrX = (printableWidth - thermalQrSize) / 2;
                    g.DrawImage(_qrBitmap, qrX, y, thermalQrSize, thermalQrSize);
                    y += thermalQrSize + 8;

                    // 4. إرشاد المسح
                    using (var fontGuide = new Font("Segoe UI", 9f, FontStyle.Bold))
                    using (var brush = new SolidBrush(Color.Black))
                    {
                        g.DrawString("امسح الرمز بكاميرا الهاتف", fontGuide, brush, new RectangleF(0, y, printableWidth, 20), sfCenter);
                        y += 20;
                        g.DrawString("وتصفح الأسعار واطلب فوراً ✨", fontGuide, brush, new RectangleF(0, y, printableWidth, 20), sfCenter);
                    }
                    y += 24;

                    // 5. رقم التواصل
                    if (!string.IsNullOrEmpty(_storePhone))
                    {
                        using (var fontPhone = new Font("Segoe UI", 8.5f, FontStyle.Bold))
                        using (var brush = new SolidBrush(Color.Black))
                        {
                            g.DrawString("واتساب / اتصال: " + _storePhone, fontPhone, brush, new RectangleF(0, y, printableWidth, 20), sfCenter);
                        }
                        y += 22;
                    }

                    // خط فاصل
                    using (var pen = new Pen(Color.Black, 1) { DashStyle = DashStyle.Dash })
                    {
                        g.DrawLine(pen, 10, y, printableWidth - 10, y);
                    }
                    y += 8;

                    // تذييل
                    using (var fontFoot = new Font("Segoe UI", 8f, FontStyle.Regular))
                    using (var brush = new SolidBrush(Color.Black))
                    {
                        g.DrawString("شكراً لتعاملكم معنا", fontFoot, brush, new RectangleF(0, y, printableWidth, 20), sfCenter);
                    }
                    y += 25;

                    ev.HasMorePages = false;
                };

                pd.Print();
                MessageBox.Show("تم إرسال إيصال كود المتجر إلى الطابعة الحرارية بنجاح ✅", "تمت الطباعة", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("حدث خطأ أثناء الطباعة الحرارية: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}

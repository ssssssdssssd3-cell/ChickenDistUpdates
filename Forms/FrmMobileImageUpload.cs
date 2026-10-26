using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;
using ChickenDist.Services;

namespace ChickenDist.Forms
{
    /// <summary>
    /// نافذة تصوير ورفع صور الأصناف من الموبايل عبر رمز QR Code
    /// تستقبل الصور الملتقطة من كاميرا الهاتف فورياً وتضغطها وتحفظها محلياً وتنشرها للمتجر
    /// </summary>
    public class FrmMobileImageUpload : Form
    {
        private readonly int _productId;
        private readonly string _productCode;
        private readonly string _productName;
        private readonly decimal _currentPrice;
        private readonly string _uploadUrl;

        public string SavedImageUrl1 { get; private set; }
        public string SavedImageUrl2 { get; private set; }
        public string SavedImageUrl3 { get; private set; }
        public decimal? ResultPrice { get; private set; }

        private PictureBox picQr;
        private Label lblStatus;
        private Button btnCopyLink;
        private Button btnOpenBrowser;
        private Button btnCancel;
        private System.Windows.Forms.Timer _pollTimer;
        private int _dotCounter = 0;
        private bool _isProcessing = false;

        public FrmMobileImageUpload(int productId, string productCode, string productName, decimal currentPrice = 0m)
        {
            _productId = productId;
            _productCode = string.IsNullOrWhiteSpace(productCode) ? "item" : productCode.Trim();
            _productName = productName ?? "صنف";
            _currentPrice = currentPrice;

            string projectId = AppConfig.FirebaseProjectId;
            if (string.IsNullOrWhiteSpace(projectId)) projectId = "prosoft";

            string encName = Uri.EscapeDataString(_productName);
            string encCode = Uri.EscapeDataString(_productCode);
            string priceStr = _currentPrice.ToString(System.Globalization.CultureInfo.InvariantCulture);

            _uploadUrl = $"https://{projectId}.web.app/upload.html?code={encCode}&id={_productId}&p={projectId}&name={encName}&price={priceStr}";

            InitUI();
            StartPolling();
        }

        private void InitUI()
        {
            this.Text = "تصوير ورفع صور الصنف من الموبايل";
            this.Size = new Size(560, 640);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.BackColor = Theme.BgMain;
            this.Font = Theme.FontMain;

            // --- Header Panel ---
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 85,
                BackColor = Theme.BgCard,
                Padding = new Padding(15, 10, 15, 10)
            };

            var lblTitle = new Label
            {
                Text = $"{_productName}   [كود: {_productCode}]",
                Font = new Font(Theme.FontMain.FontFamily, 11f, FontStyle.Bold),
                ForeColor = Theme.Primary,
                Location = new Point(15, 10),
                AutoSize = true
            };

            var lblDesc = new Label
            {
                Text = "امسح رمز الاستجابة السريعة (QR) بكاميرا هاتفك لالتقاط الصور ورفعها مباشرة للبرنامج في ثوانٍ.",
                Font = new Font(Theme.FontMain.FontFamily, 8.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(148, 163, 184),
                Location = new Point(15, 40),
                AutoSize = true
            };

            pnlHeader.Controls.AddRange(new Control[] { lblTitle, lblDesc });
            this.Controls.Add(pnlHeader);

            // --- QR Card Container ---
            var pnlQr = new Panel
            {
                Location = new Point(145, 100),
                Size = new Size(270, 270),
                BackColor = Color.White,
                Padding = new Padding(12)
            };

            picQr = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.White
            };

            try
            {
                picQr.Image = QrCodeHelper.Generate(_uploadUrl, 8, Color.FromArgb(15, 23, 42), Color.White);
            }
            catch (Exception ex)
            {
                AppLogger.Error("Generate QR Code failed in FrmMobileImageUpload", ex);
            }

            pnlQr.Controls.Add(picQr);
            this.Controls.Add(pnlQr);

            // --- Step-by-Step Instructions ---
            var lblSteps = new Label
            {
                Text = "1 وجّه كاميرا الموبايل إلى رمز الـ QR أعلاه وافتح الرابط\n" +
                       "2 التقط صور الصنف من الكاميرا أو الألبوم واضغط (حفظ ورفع فوراً)\n" +
                       "3 سيتم استلام الصور وحفظها في جهازك والمتجر الإلكتروني تلقائياً",
                Font = new Font(Theme.FontMain.FontFamily, 8.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(203, 213, 225),
                Location = new Point(30, 385),
                Size = new Size(500, 65),
                TextAlign = ContentAlignment.TopRight
            };
            this.Controls.Add(lblSteps);

            // --- Action Buttons ---
            btnCopyLink = Theme.MakeButton("نسخ رابط الرفع", 360, 465, 165, 34, Color.FromArgb(51, 65, 85));
            btnCopyLink.Click += (s, e) =>
            {
                try
                {
                    Clipboard.SetText(_uploadUrl);
                    btnCopyLink.Text = "تم النسخ!";
                    var t = new System.Windows.Forms.Timer { Interval = 2000 };
                    t.Tick += (ts, te) => { btnCopyLink.Text = "نسخ رابط الرفع"; t.Stop(); t.Dispose(); };
                    t.Start();
                }
                catch { }
            };

            btnOpenBrowser = Theme.MakeButton("فتح في المتصفح", 185, 465, 165, 34, Color.FromArgb(37, 99, 235));
            btnOpenBrowser.Click += (s, e) =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo { FileName = _uploadUrl, UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    MessageBox.Show("تعذر فتح المتصفح: " + ex.Message, "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };

            btnCancel = Theme.MakeButton("إغلاق", 30, 465, 145, 34, Color.FromArgb(100, 116, 139));
            btnCancel.Click += (s, e) =>
            {
                StopPolling();
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };

            this.Controls.AddRange(new Control[] { btnCopyLink, btnOpenBrowser, btnCancel });

            // --- Status Footer Panel ---
            var pnlStatus = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 65,
                BackColor = Theme.BgCard,
                Padding = new Padding(15, 12, 15, 12)
            };

            lblStatus = new Label
            {
                Text = "بانتظار مسح الرمز والتقاط الصور من الهاتف...",
                Font = new Font(Theme.FontMain.FontFamily, 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(250, 204, 21), // Amber
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };

            pnlStatus.Controls.Add(lblStatus);
            this.Controls.Add(pnlStatus);

            Theme.ApplyFormRTL(this);
        }

        private void StartPolling()
        {
            _pollTimer = new System.Windows.Forms.Timer { Interval = 1800 };
            _pollTimer.Tick += async (s, e) => await CheckForMobileUploadAsync();
            _pollTimer.Start();
        }

        private void StopPolling()
        {
            if (_pollTimer != null)
            {
                _pollTimer.Stop();
                _pollTimer.Dispose();
                _pollTimer = null;
            }
        }

        private async Task CheckForMobileUploadAsync()
        {
            if (_isProcessing) return;

            // Animate dots while waiting
            _dotCounter = (_dotCounter + 1) % 4;
            string dots = new string('.', _dotCounter);
            lblStatus.Text = $"بانتظار مسح الرمز والتقاط الصور من الهاتف{dots}";

            try
            {
                var upload = await CloudSyncService.FetchPendingMobileUploadAsync(_productCode);
                if (upload != null)
                {
                    _isProcessing = true;
                    StopPolling();

                    lblStatus.Text = "تم استلام الصور من الهاتف! جاري الضغط والمعالجة المحلية...";
                    lblStatus.ForeColor = Color.FromArgb(56, 189, 248); // Sky blue
                    this.Refresh();

                    await Task.Run(() =>
                    {
                        // 1. ضغط ومعالجة الصور في محرك الصور
                        if (!string.IsNullOrEmpty(upload.Image1))
                        {
                            SavedImageUrl1 = ProductImageService.SaveAndCompressFromBase64(upload.Image1, _productCode, 1);
                        }
                        if (!string.IsNullOrEmpty(upload.Image2))
                        {
                            SavedImageUrl2 = ProductImageService.SaveAndCompressFromBase64(upload.Image2, _productCode, 2);
                        }
                        if (!string.IsNullOrEmpty(upload.Image3))
                        {
                            SavedImageUrl3 = ProductImageService.SaveAndCompressFromBase64(upload.Image3, _productCode, 3);
                        }

                        // 2. تحديث السعر إذا تم إدخاله من الهاتف
                        if (upload.Price.HasValue && upload.Price.Value > 0)
                        {
                            ResultPrice = upload.Price.Value;
                            if (_productId > 0)
                            {
                                DbHelper.Execute("UPDATE Products SET SalePrice = @p WHERE ProductID = @id",
                                    DbHelper.P("@p", upload.Price.Value),
                                    DbHelper.P("@id", _productId));
                            }
                        }

                        // 3. تحديث مسارات الصور في قاعدة البيانات إذا كان الصنف محفوظاً مسبقاً
                        if (_productId > 0 && (!string.IsNullOrEmpty(SavedImageUrl1) || !string.IsNullOrEmpty(SavedImageUrl2) || !string.IsNullOrEmpty(SavedImageUrl3)))
                        {
                            ProductDAL.UpdateProductImages(_productId, SavedImageUrl1, SavedImageUrl2, SavedImageUrl3);
                        }

                        // 4. حذف النود المؤقت من Firebase بعد الاستلام بنجاح
                        CloudSyncService.DeletePendingMobileUploadAsync(_productCode).Wait(3000);

                        // 5. النشر التلقائي للويب سايت والمتجر
                        CloudSyncService.TriggerStoreCatalogSyncDebounced(1000);
                    });

                    try { System.Media.SystemSounds.Asterisk.Play(); } catch { }

                    lblStatus.Text = "تم استلام وحفظ صور الصنف بنجاح تام!";
                    lblStatus.ForeColor = Color.FromArgb(74, 222, 128); // Emerald Green
                    this.Refresh();

                    // إغلاق النافذة بنجاح تلقائياً بعد ثانية واحدة
                    var closeTimer = new System.Windows.Forms.Timer { Interval = 1100 };
                    closeTimer.Tick += (cs, ce) =>
                    {
                        closeTimer.Stop();
                        closeTimer.Dispose();
                        this.DialogResult = DialogResult.OK;
                        this.Close();
                    };
                    closeTimer.Start();
                }
            }
            catch (Exception ex)
            {
                _isProcessing = false;
                AppLogger.Warn($"FrmMobileImageUpload polling warning: {ex.Message}", "FrmMobileImageUpload");
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            StopPolling();
            base.OnFormClosing(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                StopPolling();
                if (picQr?.Image != null) picQr.Image.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}

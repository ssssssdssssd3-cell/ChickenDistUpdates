using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using ChickenDist.Core;
using ChickenDist.DAL;
using ChickenDist.Services;

namespace ChickenDist.Forms
{
    public class FrmProductImages : Form
    {
        private int _productId;
        private string _productCode;
        private string _productName;

        public string ImageUrl1 { get; private set; }
        public string ImageUrl2 { get; private set; }
        public string ImageUrl3 { get; private set; }

        private PictureBox picSlot1, picSlot2, picSlot3;
        private Label lblSlot1Info, lblSlot2Info, lblSlot3Info;
        private Button btnSlot1Choose, btnSlot1Paste, btnSlot1Url, btnSlot1Delete;
        private Button btnSlot2Choose, btnSlot2Paste, btnSlot2Url, btnSlot2Delete;
        private Button btnSlot3Choose, btnSlot3Paste, btnSlot3Url, btnSlot3Delete;

        private Button btnSave, btnCancel;

        public FrmProductImages(int productId, string productCode, string productName, string img1, string img2, string img3)
        {
            _productId = productId;
            _productCode = string.IsNullOrWhiteSpace(productCode) ? "item" : productCode.Trim();
            _productName = productName ?? "";

            ImageUrl1 = img1 ?? "";
            ImageUrl2 = img2 ?? "";
            ImageUrl3 = img3 ?? "";

            InitUI();
            LoadExistingImages();
        }

        private void InitUI()
        {
            this.Text = "📷 إدارة صور الصنف للمتجر الإلكتروني (الحد الأقصى 3 صور)";
            this.Size = new Size(820, 540);
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
                Text = $"📦 صنف: {_productName}  [كود: {_productCode}]",
                Font = new Font(Theme.FontMain.FontFamily, 11f, FontStyle.Bold),
                ForeColor = Theme.Primary,
                Location = new Point(15, 10),
                AutoSize = true
            };

            var lblTip = new Label
            {
                Text = "⚡ حماية باقة الإنترنت: يتم تصغير وضغط كل صورة تلقائياً لحجم فائق الخفة (~25-35 KB) لتحميل فوري دون استهلاك الباقة.",
                Font = new Font(Theme.FontMain.FontFamily, 8.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(74, 222, 128), // Soft Emerald
                Location = new Point(15, 42),
                AutoSize = true
            };

            var lblTip2 = new Label
            {
                Text = "• الصورة 1: الغلاف الأساسي بالمتجر  • الصور 2 و 3: معرض التفاصيل (يُحمل فقط عند طلب العميل)",
                Font = new Font(Theme.FontMain.FontFamily, 8.25f, FontStyle.Regular),
                ForeColor = Color.FromArgb(148, 163, 184),
                Location = new Point(15, 62),
                AutoSize = true
            };

            pnlHeader.Controls.AddRange(new Control[] { lblTitle, lblTip, lblTip2 });
            this.Controls.Add(pnlHeader);

            // --- Content Panel (3 Slots) ---
            var pnlContent = new Panel
            {
                Location = new Point(10, 95),
                Size = new Size(785, 360),
                BackColor = Color.Transparent
            };

            int slotWidth = 250;
            int gap = 12;

            // Slot 1: الغلاف
            var grp1 = CreateSlotGroup("⭐ 1. الصورة الرئيسية (الغلاف)", 5, slotWidth,
                out picSlot1, out lblSlot1Info, out btnSlot1Choose, out btnSlot1Paste, out btnSlot1Url, out btnSlot1Delete,
                1);
            pnlContent.Controls.Add(grp1);

            // Slot 2: صورة إضافية 1
            var grp2 = CreateSlotGroup("🖼️ 2. صورة إضافية (معرض)", 5 + slotWidth + gap, slotWidth,
                out picSlot2, out lblSlot2Info, out btnSlot2Choose, out btnSlot2Paste, out btnSlot2Url, out btnSlot2Delete,
                2);
            pnlContent.Controls.Add(grp2);

            // Slot 3: صورة إضافية 2
            var grp3 = CreateSlotGroup("🖼️ 3. صورة إضافية (معرض)", 5 + (slotWidth + gap) * 2, slotWidth,
                out picSlot3, out lblSlot3Info, out btnSlot3Choose, out btnSlot3Paste, out btnSlot3Url, out btnSlot3Delete,
                3);
            pnlContent.Controls.Add(grp3);

            this.Controls.Add(pnlContent);

            // --- Footer Panel ---
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Theme.BgCard
            };

            btnSave = Theme.MakeButton("💾 اعتماد وحفظ الصور", 430, 12, 190, 36, Theme.Accent);
            btnSave.Click += BtnSave_Click;

            btnCancel = Theme.MakeButton("❌ إلغاء", 290, 12, 130, 36, Color.FromArgb(100, 110, 120));
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            pnlFooter.Controls.AddRange(new Control[] { btnSave, btnCancel });
            this.Controls.Add(pnlFooter);

            Theme.ApplyFormRTL(this);
        }

        private GroupBox CreateSlotGroup(string title, int x, int width,
            out PictureBox pic, out Label lblInfo, out Button btnChoose, out Button btnPaste, out Button btnUrl, out Button btnDel,
            int slotNumber)
        {
            var grp = new GroupBox
            {
                Text = title,
                Location = new Point(x, 5),
                Size = new Size(width, 345),
                ForeColor = slotNumber == 1 ? Color.FromArgb(250, 204, 21) : Theme.Primary,
                Font = new Font(Theme.FontMain, FontStyle.Bold),
                BackColor = Color.FromArgb(24, 32, 47)
            };

            pic = new PictureBox
            {
                Location = new Point(15, 25),
                Size = new Size(width - 30, 160),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(15, 23, 42),
                BorderStyle = BorderStyle.FixedSingle
            };
            grp.Controls.Add(pic);

            lblInfo = new Label
            {
                Text = "لا توجد صورة",
                Location = new Point(10, 190),
                Size = new Size(width - 20, 20),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font(Theme.FontMain.FontFamily, 8f, FontStyle.Regular),
                ForeColor = Color.FromArgb(148, 163, 184)
            };
            grp.Controls.Add(lblInfo);

            int btnW = (width - 35) / 2;
            int by1 = 215;
            int by2 = 255;

            btnChoose = Theme.MakeButton("📂 اختيار ملف", 15, by1, btnW, 32, Color.FromArgb(37, 99, 235));
            btnPaste = Theme.MakeButton("📋 لصق حافظة", 20 + btnW, by1, btnW, 32, Color.FromArgb(59, 130, 246));

            btnUrl = Theme.MakeButton("🌐 رابط مباشر", 15, by2, btnW, 32, Color.FromArgb(71, 85, 105));
            btnDel = Theme.MakeButton("🗑️ حذف الصورة", 20 + btnW, by2, btnW, 32, Color.FromArgb(225, 29, 72));

            int curSlot = slotNumber;
            btnChoose.Click += (s, e) => HandleChooseFile(curSlot);
            btnPaste.Click += (s, e) => HandlePasteClipboard(curSlot);
            btnUrl.Click += (s, e) => HandleDirectUrl(curSlot);
            btnDel.Click += (s, e) => HandleDeleteImage(curSlot);

            grp.Controls.AddRange(new Control[] { btnChoose, btnPaste, btnUrl, btnDel });
            return grp;
        }

        private void LoadExistingImages()
        {
            UpdateSlotDisplay(1, ImageUrl1);
            UpdateSlotDisplay(2, ImageUrl2);
            UpdateSlotDisplay(3, ImageUrl3);
        }

        private void UpdateSlotDisplay(int slot, string pathOrUrl)
        {
            PictureBox pic = slot == 1 ? picSlot1 : slot == 2 ? picSlot2 : picSlot3;
            Label lbl = slot == 1 ? lblSlot1Info : slot == 2 ? lblSlot2Info : lblSlot3Info;

            if (string.IsNullOrWhiteSpace(pathOrUrl))
            {
                if (pic.Image != null) { var old = pic.Image; pic.Image = null; old.Dispose(); }
                lbl.Text = "⚪ لا توجد صورة";
                lbl.ForeColor = Color.FromArgb(148, 163, 184);
                return;
            }

            try
            {
                var img = ProductImageService.LoadImageSafe(pathOrUrl);
                if (img != null)
                {
                    if (pic.Image != null) { var old = pic.Image; pic.Image = null; old.Dispose(); }
                    pic.Image = img;
                    string sizeStr = ProductImageService.GetFileSizeString(pathOrUrl);
                    lbl.Text = $"✅ {img.Width}x{img.Height} px {(string.IsNullOrEmpty(sizeStr) ? "" : $"| {sizeStr}")}";
                    lbl.ForeColor = Color.FromArgb(74, 222, 128);
                }
                else
                {
                    lbl.Text = "⚠️ تعذر معاينة الصورة محلياً";
                    lbl.ForeColor = Color.FromArgb(251, 191, 36);
                }
            }
            catch (Exception ex)
            {
                lbl.Text = "❌ خطأ في المعاينة: " + ex.Message;
                lbl.ForeColor = Color.FromArgb(248, 113, 113);
            }
        }

        private void HandleChooseFile(int slot)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Title = $"اختيار صورة الصنف - خانة {slot}";
                ofd.Filter = "ملفات الصور (*.jpg;*.jpeg;*.png;*.webp;*.bmp)|*.jpg;*.jpeg;*.png;*.webp;*.bmp|جميع الملفات (*.*)|*.*";
                if (ofd.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        string savedRelPath = ProductImageService.SaveAndCompressFromFile(ofd.FileName, _productCode, slot);
                        if (!string.IsNullOrEmpty(savedRelPath))
                        {
                            SetSlotUrl(slot, savedRelPath);
                            UpdateSlotDisplay(slot, savedRelPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("فشل حفظ ومعالجة الصورة: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void HandlePasteClipboard(int slot)
        {
            try
            {
                if (Clipboard.ContainsImage())
                {
                    using (Image clipImg = Clipboard.GetImage())
                    {
                        if (clipImg != null)
                        {
                            string savedRelPath = ProductImageService.SaveAndCompressImage(clipImg, _productCode, slot);
                            if (!string.IsNullOrEmpty(savedRelPath))
                            {
                                SetSlotUrl(slot, savedRelPath);
                                UpdateSlotDisplay(slot, savedRelPath);
                                MessageBox.Show("✅ تم لصق الصورة وضغطها بنجاح!", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                return;
                            }
                        }
                    }
                }
                else if (Clipboard.ContainsFileDropList())
                {
                    var files = Clipboard.GetFileDropList();
                    if (files.Count > 0 && File.Exists(files[0]))
                    {
                        string savedRelPath = ProductImageService.SaveAndCompressFromFile(files[0], _productCode, slot);
                        if (!string.IsNullOrEmpty(savedRelPath))
                        {
                            SetSlotUrl(slot, savedRelPath);
                            UpdateSlotDisplay(slot, savedRelPath);
                            MessageBox.Show("✅ تم استيراد الصورة من الملف المنسوخ وضغطها بنجاح!", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }
                    }
                }

                MessageBox.Show("⚠️ لا توجد صورة في الحافظة!\nيرجى نسخ صورة (أو استخدام أداة التقاط الشاشة Snipping Tool ثم نسخ) والمحاولة مرة أخرى.", "تنبيه الحافظة", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show("فشل لصق الصورة: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void HandleDirectUrl(int slot)
        {
            string currentUrl = GetSlotUrl(slot);
            string input = ShowUrlInputDialog(
                $"رابط صورة الصنف (خانة {slot})",
                "أدخل رابط الصورة المباشر من الإنترنت (HTTPS):",
                currentUrl);

            if (input == null) return; // User pressed Cancel
            input = input.Trim();

            if (string.IsNullOrWhiteSpace(input))
            {
                SetSlotUrl(slot, "");
                UpdateSlotDisplay(slot, "");
                return;
            }

            if (!input.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !input.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
                !input.StartsWith("images/", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("يرجى إدخال رابط يبدأ بـ https:// أو مسار images/products/", "تنبيه الرابط", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SetSlotUrl(slot, input);
            UpdateSlotDisplay(slot, input);
        }

        private string ShowUrlInputDialog(string title, string prompt, string defaultValue)
        {
            using (var form = new Form())
            {
                form.Text = title;
                form.Size = new Size(500, 180);
                form.StartPosition = FormStartPosition.CenterParent;
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.MaximizeBox = false;
                form.MinimizeBox = false;
                form.BackColor = Theme.BgMain;
                form.ForeColor = Theme.TextMain;
                form.Font = Theme.FontMain;
                form.RightToLeft = RightToLeft.Yes;
                form.RightToLeftLayout = true;

                var lbl = new Label { Text = prompt, Location = new Point(15, 15), Size = new Size(450, 25), AutoSize = false };
                var txt = new TextBox { Text = defaultValue ?? "", Location = new Point(15, 45), Size = new Size(450, 26), BackColor = Theme.BgInput, ForeColor = Theme.TextMain };
                var btnOk = Theme.MakeButton("✅ موافق", 260, 85, 100, 32, Theme.Accent);
                var btnCancel = Theme.MakeButton("❌ إلغاء", 140, 85, 100, 32, Color.FromArgb(100, 110, 120));

                btnOk.DialogResult = DialogResult.OK;
                btnCancel.DialogResult = DialogResult.Cancel;
                form.AcceptButton = btnOk;
                form.CancelButton = btnCancel;

                form.Controls.AddRange(new Control[] { lbl, txt, btnOk, btnCancel });

                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    return txt.Text.Trim();
                }
                return null;
            }
        }

        private void HandleDeleteImage(int slot)
        {
            string currentUrl = GetSlotUrl(slot);
            if (string.IsNullOrWhiteSpace(currentUrl)) return;

            var dr = MessageBox.Show($"هل أنت متأكد من حذف الصورة من الخانة {slot}؟", "تأكيد الحذف", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dr == DialogResult.Yes)
            {
                ProductImageService.DeleteProductImage(currentUrl);
                SetSlotUrl(slot, "");
                UpdateSlotDisplay(slot, "");
            }
        }

        private string GetSlotUrl(int slot)
        {
            return slot == 1 ? ImageUrl1 : slot == 2 ? ImageUrl2 : ImageUrl3;
        }

        private void SetSlotUrl(int slot, string url)
        {
            if (slot == 1) ImageUrl1 = url;
            else if (slot == 2) ImageUrl2 = url;
            else if (slot == 3) ImageUrl3 = url;
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (_productId > 0)
                {
                    ProductDAL.UpdateProductImages(_productId, ImageUrl1, ImageUrl2, ImageUrl3);
                }

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("فشل تحديث صور الصنف: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (picSlot1?.Image != null) picSlot1.Image.Dispose();
                if (picSlot2?.Image != null) picSlot2.Image.Dispose();
                if (picSlot3?.Image != null) picSlot3.Image.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}

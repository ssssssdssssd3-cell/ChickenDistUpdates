using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using ChickenDist.Core;

namespace ChickenDist.Forms
{
    /// <summary>
    /// نافذة دليل وبرمجة أجهزة الاسكانر لحل تداخل الباركود الدولي و S/N وقراءة IMEI فقط
    /// </summary>
    public class FrmScannerConfigGuide : Form
    {
        private Button btnPrintGuide;
        private Button btnClose;

        public FrmScannerConfigGuide()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "🛠️ دليل وبرمجة أجهزة الاسكانر — قراءة IMEI فقط وتجاهل الكود الدولي";
            this.Size = new Size(820, 680);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.Font = new Font("Segoe UI", 9.5f);
            this.BackColor = Color.FromArgb(248, 250, 252);

            // ── رأس النافذة ─────────────────────────────────────────────────────────
            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = Color.FromArgb(15, 23, 42),
                Padding = new Padding(20, 12, 20, 10)
            };

            var lblHeaderTitle = new Label
            {
                Text = "📱 حل تداخل قراءة الباركود في نشاط الموبايلات",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                Location = new Point(20, 12),
                AutoSize = true
            };

            var lblHeaderSub = new Label
            {
                Text = "توجيه جهاز الاسكانر لقراءة IMEI 1 فقط (15 رقم) وتجاوز الباركود الدولي و S/N المطبوعين على العلبة",
                ForeColor = Color.FromArgb(148, 163, 184),
                Font = new Font("Segoe UI", 9.2f),
                Location = new Point(20, 42),
                AutoSize = true
            };

            pnlTop.Controls.Add(lblHeaderTitle);
            pnlTop.Controls.Add(lblHeaderSub);

            // ── لوحة الأزرار السفلية ───────────────────────────────────────────────
            var pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.White,
                Padding = new Padding(20, 10, 20, 10)
            };

            btnPrintGuide = new Button
            {
                Text = "🖨️ طباعة ورقة باركودات ضبط الاسكانر (A4 / طابعة)",
                Dock = DockStyle.Left,
                Width = 320,
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnPrintGuide.FlatAppearance.BorderSize = 0;
            btnPrintGuide.Click += BtnPrintGuide_Click;

            btnClose = new Button
            {
                Text = "إغلاق",
                Dock = DockStyle.Right,
                Width = 100,
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(71, 85, 105),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => this.Close();

            pnlBottom.Controls.Add(btnPrintGuide);
            pnlBottom.Controls.Add(btnClose);

            // ── المحتوى الرئيسي داخل Scrollable Panel ─────────────────────────────
            var pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(20, 15, 20, 15)
            };

            int curY = 10;

            // كارت 1: الحل الأسهل والأمثل (استيكر IMEI الموحد)
            var pnlCard1 = CreateCard("🎯 الطريقة الأولى (الأسهل والأدق): استخدام استيكر IMEI الموحد للمحل",
                "• تم تزويد البرنامج بنموذج طباعة مخصص للهواتف (استيكر IMEI الموحد).\n" +
                "• عند استلام كراتين الموبايل، اضغط زر [🖨️ طباعة الباركود] لطباعة ملصق حراري لكل جهاز.\n" +
                "• الاستيكر يحتوي على: اسم المحل بخط كبير وواضح + اسم الجهاز + باركود واحد فقط للـ IMEI.\n" +
                "• بمجرد لصقه على الكرتونة، يقرأه أي اسكانر في العالم في أقل من جزء من الثانية وبدون أي تشتيت!",
                Color.FromArgb(238, 242, 255), Color.FromArgb(79, 70, 229));
            pnlCard1.Location = new Point(15, curY);
            pnlCard1.Width = 750;
            pnlBody.Controls.Add(pnlCard1);
            curY += pnlCard1.Height + 15;

            // كارت 2: برمجة جهاز الاسكانر نفسه (Hardware Configuration)
            var pnlCard2 = CreateCard("⚙️ الطريقة الثانية (الجذرية): برمجة الاسكانر ليتجاهل الكود الدولي هاردوير",
                "أجهزة الاسكانر الحديثة تحتوي على شريحة داخلية يمكن برمجتها بمسح باركود من الكتالوج:\n" +
                "1. تعطيل قراءة الباركود الدولي (Disable EAN-13 / UPC): الاسكانر يصبح أعمى عن كود العلبة الدولي ولا يرسله للكمبيوتر نهائياً.\n" +
                "2. رفع الحد الأدنى لطول الكود إلى 14 رقماً (Min Code 128 Length = 14): الاسكانر يتجاهل كود S/N ولا يقرأ إلا الـ IMEI (15 رقم).\n" +
                "3. تفعيل نمط التوجيه النقطي (Picklist Mode): يقرأ فقط الباركود الذي تقع عليه النقطة الحمراء مباشرة.",
                Color.FromArgb(240, 253, 244), Color.FromArgb(22, 163, 74));
            pnlCard2.Location = new Point(15, curY);
            pnlCard2.Width = 750;
            pnlBody.Controls.Add(pnlCard2);
            curY += pnlCard2.Height + 15;

            // كارت 3: عرض باركودات البرمجة السريعة على الشاشة
            var pnlBarcodes = new Panel
            {
                Location = new Point(15, curY),
                Width = 750,
                Height = 310,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(12)
            };

            var lblBcTitle = new Label
            {
                Text = "⚡ باركودات ضبط الاسكانر (يمكنك توجيه الاسكانر ومسحها مباشرة من الشاشة):",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(12, 10),
                AutoSize = true
            };
            pnlBarcodes.Controls.Add(lblBcTitle);

            // عنصر باركود 1: تعطيل EAN-13
            var cardBc1 = CreateBarcodeDisplayBox("1. تعطيل الباركود الدولي (Disable EAN-13)", "%%SpecCode68", "يجعل الاسكانر يتجاهل كود الكرتونة الدولي نهائياً", 15, 40);
            pnlBarcodes.Controls.Add(cardBc1);

            // عنصر باركود 2: تحديد طول الكود 15 رقماً
            var cardBc2 = CreateBarcodeDisplayBox("2. حصر القراءة على 15 رقماً (Min Length 14)", "%%SpecCode15", "يتجاهل كود السيريال S/N القصير", 380, 40);
            pnlBarcodes.Controls.Add(cardBc2);

            // عنصر باركود 3: وضع التوجيه النقطي Picklist Mode
            var cardBc3 = CreateBarcodeDisplayBox("3. تفعيل التصويب النقطي (Picklist Mode)", "%%SpecCodePL", "يقرأ الباركود الواقع تحت نقطة الليزر فقط", 15, 175);
            pnlBarcodes.Controls.Add(cardBc3);

            // عنصر باركود 4: استعادة ضبط المصنع
            var cardBc4 = CreateBarcodeDisplayBox("4. استعادة ضبط المصنع للاسكانر", "%%SpecCodeReset", "لإعادة الاسكانر لقراءة جميع الباركودات كالسابق", 380, 175);
            pnlBarcodes.Controls.Add(cardBc4);

            pnlBody.Controls.Add(pnlBarcodes);
            curY += pnlBarcodes.Height + 15;

            // كارت 4: نصائح سريعة للاسكانر
            var pnlCard4 = CreateCard("💡 نصيحة سريعة أثناء المسح على العلبة الأصلية",
                "• الفلترة الذكية في البرنامج أصبحت الآن صامتة وفورية 100%: عند مرور شعاع الاسكانر على الكود الدولي أو S/N، يتم تجاوزهما تلقائياً وبدون صوت خطأ، وبمجرد لمس شعاع الليزر لكود IMEI 1 يتم تسجيله فوراً.\n" +
                "• يمكنك وضع إصبعك أو لاصق ورقي صغير فوق الكود الدولي على الكرتونة لمنع شعاع الليزر من التقاطه نهائياً.",
                Color.FromArgb(254, 243, 199), Color.FromArgb(217, 119, 6));
            pnlCard4.Location = new Point(15, curY);
            pnlCard4.Width = 750;
            pnlBody.Controls.Add(pnlCard4);
            curY += pnlCard4.Height + 20;

            this.Controls.Add(pnlBody);
            this.Controls.Add(pnlBottom);
            this.Controls.Add(pnlTop);
        }

        private Panel CreateCard(string title, string content, Color backColor, Color borderColor)
        {
            var pnl = new Panel
            {
                BackColor = backColor,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(14)
            };

            var lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = borderColor,
                Dock = DockStyle.Top,
                Height = 26,
                AutoSize = false
            };

            var lblContent = new Label
            {
                Text = content,
                Font = new Font("Segoe UI", 9.2f),
                ForeColor = Color.FromArgb(51, 65, 85),
                Dock = DockStyle.Fill,
                AutoSize = false
            };

            pnl.Controls.Add(lblContent);
            pnl.Controls.Add(lblTitle);

            // قياس الارتفاع المناسب للنص
            using (var g = this.CreateGraphics())
            {
                var sz = g.MeasureString(content, lblContent.Font, 710);
                pnl.Height = (int)sz.Height + 48;
            }

            return pnl;
        }

        private Panel CreateBarcodeDisplayBox(string labelText, string barcodeValue, string note, int x, int y)
        {
            var pnl = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(350, 120),
                BackColor = Color.FromArgb(248, 250, 252),
                BorderStyle = BorderStyle.FixedSingle
            };

            var lbl = new Label
            {
                Text = labelText,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(8, 6),
                AutoSize = true
            };

            var pb = new PictureBox
            {
                Location = new Point(15, 28),
                Size = new Size(318, 55),
                BackColor = Color.White,
                SizeMode = PictureBoxSizeMode.CenterImage
            };

            // رسم الباركود برمجياً داخل الصورة
            try
            {
                var bmp = new Bitmap(pb.Width, pb.Height);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.White);
                    BarcodeEngine.DrawBarcode(g, barcodeValue, "Code128", 10, 5, pb.Width - 20, 36);
                    using (var f = new Font("Consolas", 8f))
                    {
                        var sf = new StringFormat { Alignment = StringAlignment.Center };
                        g.DrawString(barcodeValue, f, Brushes.Black, new RectangleF(0, 41, pb.Width, 14), sf);
                    }
                }
                pb.Image = bmp;
            }
            catch { }

            var lblNote = new Label
            {
                Text = note,
                Font = new Font("Segoe UI", 7.8f),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(8, 88),
                AutoSize = true
            };

            pnl.Controls.Add(lbl);
            pnl.Controls.Add(pb);
            pnl.Controls.Add(lblNote);
            return pnl;
        }

        private void BtnPrintGuide_Click(object sender, EventArgs e)
        {
            try
            {
                var pd = new PrintDocument();
                pd.DefaultPageSettings.PaperSize = new PaperSize("A4", 827, 1169);
                pd.PrintPage += Pd_PrintPage;

                using (var dlg = new PrintPreviewDialog())
                {
                    dlg.Document = pd;
                    dlg.WindowState = FormWindowState.Maximized;
                    dlg.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("حدث خطأ أثناء معاينة طباعة الدليل:\n" + ex.Message, "خطأ في الطباعة", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Pd_PrintPage(object sender, PrintPageEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Color.White);

            var titleFont = new Font("Segoe UI", 16f, FontStyle.Bold);
            var subFont = new Font("Segoe UI", 10f);
            var secFont = new Font("Segoe UI", 12f, FontStyle.Bold);
            var bodyFont = new Font("Segoe UI", 9.5f);
            var boldFont = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            var centerFormat = new StringFormat { Alignment = StringAlignment.Center };
            var rightFormat = new StringFormat { Alignment = StringAlignment.Far, FormatFlags = StringFormatFlags.DirectionRightToLeft };

            float y = 40;
            float margin = 40;
            float printableWidth = e.PageBounds.Width - (margin * 2);

            // رأس الصفحة
            g.DrawString("📱 ورقة ضبط وبرمجة أجهزة الاسكانر — نشاط الموبايلات", titleFont, Brushes.Navy, new RectangleF(margin, y, printableWidth, 30), centerFormat);
            y += 35;
            g.DrawString("توجيه الاسكانر لقراءة كود IMEI 1 فقط (15 رقم) وتجاوز الكود الدولي و S/N", subFont, Brushes.Gray, new RectangleF(margin, y, printableWidth, 20), centerFormat);
            y += 30;

            g.DrawLine(Pens.LightGray, margin, y, e.PageBounds.Width - margin, y);
            y += 20;

            // قسم 1: استيكر IMEI الموحد
            g.DrawString("1. الحل الأمثل: طباعة استيكر IMEI الموحد لكل جهاز", secFont, Brushes.DarkBlue, new RectangleF(margin, y, printableWidth, 25), rightFormat);
            y += 26;
            string textSec1 = "عند استلام الأجهزة بالمحل، يمكنك طباعة ملصق حراري مخصص (IMEI) من البرنامج ولصقه على العلبة.\n" +
                              "يحتوي الملصق على اسم المحل واسم الجهاز ورقم IMEI 1 بباركود واحد فقط، فيقرأه الاسكانر في جزء من الثانية وبدون أي تشتيت.";
            g.DrawString(textSec1, bodyFont, Brushes.Black, new RectangleF(margin, y, printableWidth, 45), rightFormat);
            y += 55;

            g.DrawLine(Pens.LightGray, margin, y, e.PageBounds.Width - margin, y);
            y += 20;

            // قسم 2: باركودات البرمجة
            g.DrawString("2. باركودات ضبط جهاز الاسكانر (وجّه الاسكانر نحو الباركود واضغط الزر):", secFont, Brushes.DarkBlue, new RectangleF(margin, y, printableWidth, 25), rightFormat);
            y += 35;

            DrawPrintBarcodeBlock(g, "أ) تعطيل قراءة الباركود الدولي (Disable EAN-13):", "%%SpecCode68", "يجعل الاسكانر أعمى تماماً عن باركود الكرتونة الدولي 6936...", margin, ref y, printableWidth);
            y += 15;
            DrawPrintBarcodeBlock(g, "ب) تحديد الحد الأدنى لطول الكود بـ 14 رقماً (Min Length = 14):", "%%SpecCode15", "يتجاهل كود السيريال S/N القصير ولا يقرأ إلا كود IMEI (15 رقم)", margin, ref y, printableWidth);
            y += 15;
            DrawPrintBarcodeBlock(g, "ج) تفعيل وضع التوجيه النقطي (Picklist / Center Aiming Mode):", "%%SpecCodePL", "للأجهزة 2D: يقرأ فقط الباركود الواقع تحت النقطة الحمراء", margin, ref y, printableWidth);
            y += 15;
            DrawPrintBarcodeBlock(g, "د) استعادة إعدادات المصنع للاسكانر (Factory Reset):", "%%SpecCodeReset", "لإعادة الاسكانر للوضع الطبيعي وقراءة جميع الباركودات كالسابق", margin, ref y, printableWidth);
            y += 30;

            // قسم 3: نصائح سريعة
            g.DrawLine(Pens.LightGray, margin, y, e.PageBounds.Width - margin, y);
            y += 20;
            g.DrawString("3. نصيحة عملية وسريعة:", secFont, Brushes.DarkGreen, new RectangleF(margin, y, printableWidth, 25), rightFormat);
            y += 26;
            string textSec3 = "• إذا كان الاسكانر يقرأ الكود الدولي قبل IMEI، يمكنك وضع إصبعك أو ملصق ورقي صغير فوق الكود الدولي على الكرتونة ليقرأ الاسكانر كود IMEI فوراً وبدون تردد.\n" +
                              "• نظام البرنامج يتجاهل الكود الدولي و S/N تلقائياً وبصمت تام لتسريع عملية القراءة.";
            g.DrawString(textSec3, boldFont, Brushes.DarkSlateGray, new RectangleF(margin, y, printableWidth, 50), rightFormat);

            e.HasMorePages = false;
        }

        private void DrawPrintBarcodeBlock(Graphics g, string title, string code, string description, float margin, ref float y, float printableWidth)
        {
            var titleFont = new Font("Segoe UI", 10f, FontStyle.Bold);
            var descFont = new Font("Segoe UI", 8.5f);
            var codeFont = new Font("Consolas", 8.5f);
            var rightFormat = new StringFormat { Alignment = StringAlignment.Far, FormatFlags = StringFormatFlags.DirectionRightToLeft };
            var centerFormat = new StringFormat { Alignment = StringAlignment.Center };

            g.DrawString(title, titleFont, Brushes.Black, new RectangleF(margin, y, printableWidth, 20), rightFormat);
            y += 22;

            float bcWidth = 260;
            float bcHeight = 45;
            float bcX = (e_PageWidth(printableWidth, margin) - bcWidth) / 2;

            BarcodeEngine.DrawBarcode(g, code, "Code128", bcX, y, bcWidth, bcHeight);
            y += bcHeight + 3;

            g.DrawString(code, codeFont, Brushes.Black, new RectangleF(margin, y, printableWidth, 16), centerFormat);
            y += 18;

            g.DrawString(description, descFont, Brushes.DimGray, new RectangleF(margin, y, printableWidth, 18), rightFormat);
            y += 20;
        }

        private float e_PageWidth(float printableWidth, float margin)
        {
            return printableWidth + (margin * 2);
        }
    }
}

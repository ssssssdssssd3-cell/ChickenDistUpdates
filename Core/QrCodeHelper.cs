using System;
using System.Drawing;
using System.IO;
using System.Net;
using QRCoder;

namespace ChickenDist.Core
{
    /// <summary>
    /// فئة مركزية متقدمة لتوليد باركود ورموز الاستجابة السريعة (QR Code) بأعلى دقة
    /// مع آليات احتياطية متكاملة تمنع أي أخطاء أو توقف في حال فقدان ملفات أو تعذر النظام
    /// </summary>
    public static class QrCodeHelper
    {
        /// <summary>
        /// توليد صورة QR كـ Bitmap عالية الدقة مع حماية شاملة من الأخطاء
        /// </summary>
        public static Bitmap Generate(string content, int pixelsPerModule = 10, Color? darkColor = null, Color? lightColor = null)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                string projectId = AppConfig.Get("FirebaseProjectId", "checkin-192ab");
                if (string.IsNullOrEmpty(projectId)) projectId = "checkin-192ab";
                content = $"https://{projectId}.web.app/store.html";
            }

            Color dark = darkColor ?? Color.Black;
            Color light = lightColor ?? Color.White;

            // 1. المحاولة الأولى: عبر مكتبة QRCoder المضمنة محلياً
            try
            {
                using (var qrGen = new QRCodeGenerator())
                {
                    var qrData = qrGen.CreateQrCode(content, QRCodeGenerator.ECCLevel.H);
                    using (var qrCode = new QRCode(qrData))
                    {
                        return qrCode.GetGraphic(pixelsPerModule, dark, light, true);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Local QRCoder error: " + ex.Message);
            }

            // 2. المحاولة الثانية: خدمة QR السحابية السريعة (في حال توفر الإنترنت)
            try
            {
                string encoded = Uri.EscapeDataString(content);
                int sizePx = Math.Max(300, pixelsPerModule * 30);
                string apiUrl = $"https://api.qrserver.com/v1/create-qr-code/?size={sizePx}x{sizePx}&data={encoded}";
                
                var req = (HttpWebRequest)WebRequest.Create(apiUrl);
                req.Timeout = 3000;
                req.ReadWriteTimeout = 3000;
                using (var resp = req.GetResponse())
                using (var stream = resp.GetResponseStream())
                {
                    if (stream != null)
                    {
                        return new Bitmap(stream);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Online QR fallback error: " + ex.Message);
            }

            // 3. المحاولة الثالثة: بطاقة بديلة نظيفة تحتوي على الرابط بشكل مرئي دون أي انهيار
            try
            {
                int cardSize = 400;
                var bmp = new Bitmap(cardSize, cardSize);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.Clear(light);
                    using (var borderPen = new Pen(dark, 3))
                    {
                        g.DrawRectangle(borderPen, 10, 10, cardSize - 20, cardSize - 20);
                    }

                    using (var fontHeader = new Font("Segoe UI", 13f, FontStyle.Bold))
                    using (var fontText = new Font("Segoe UI", 9.5f, FontStyle.Regular))
                    using (var brush = new SolidBrush(dark))
                    {
                        var sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                        g.DrawString("📱 رابط المنيو / المتجر الإلكتروني", fontHeader, brush, new RectangleF(15, 40, cardSize - 30, 40), sfCenter);
                        g.DrawString(content, fontText, brush, new RectangleF(25, 120, cardSize - 50, 120), sfCenter);
                        g.DrawString("(امسح الرابط أو افتحه بالمتصفح)", fontText, Brushes.Gray, new RectangleF(15, 260, cardSize - 30, 30), sfCenter);
                    }
                }
                return bmp;
            }
            catch
            {
                return new Bitmap(200, 200);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using ChickenDist.Core;

namespace ChickenDist.Services
{
    public static class ProductImageService
    {
        public const int Slot1MaxSize = 450;
        public const int Slot2MaxSize = 600;
        public const int Slot3MaxSize = 600;
        public const long JpegQuality = 75L;

        /// <summary>
        /// استخراج مسارات المجلدات المحلية النشطة لتخزين ومزامنة الصور (MobileApp و bot/public).
        /// </summary>
        public static List<string> GetTargetDirectories()
        {
            var dirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            string baseDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            
            // 1. المجلد الأساسي للتطبيق
            dirs.Add(Path.Combine(baseDir, "MobileApp", "images", "products"));

            // 2. إذا كنا نعمل في بيئة التطوير (bin/Release/net48) نصل للجذر
            string cur = baseDir;
            for (int i = 0; i < 4; i++)
            {
                if (string.IsNullOrEmpty(cur)) break;
                string candidateProj = Path.Combine(cur, "MobileApp", "images", "products");
                if (Directory.Exists(Path.Combine(cur, "MobileApp")))
                {
                    dirs.Add(candidateProj);
                    string candidateBot = Path.Combine(cur, "bot", "public", "images", "products");
                    dirs.Add(candidateBot);
                    break;
                }
                var parent = Directory.GetParent(cur);
                cur = parent?.FullName;
            }

            // 3. مسار FINAL_RELEASE إذا كان موجوداً
            string finalRelease = @"D:\قطع غيار وتوزيع\قطع غيار وتوزيع\FINAL_RELEASE\ChickenDist_Program\MobileApp\images\products";
            if (Directory.Exists(Path.GetDirectoryName(Path.GetDirectoryName(finalRelease))))
            {
                dirs.Add(finalRelease);
            }

            // 4. مسار D:\prosoft
            string prosoftDir = @"D:\prosoft\MobileApp\images\products";
            if (Directory.Exists(@"D:\prosoft\MobileApp"))
            {
                dirs.Add(prosoftDir);
            }

            // التأكد من إنشاء كافة المجلدات
            foreach (var d in dirs)
            {
                try
                {
                    if (!Directory.Exists(d)) Directory.CreateDirectory(d);
                }
                catch { }
            }

            return dirs.ToList();
        }

        /// <summary>
        /// تنظيف كود الصنف ليكون اسم ملف آمن
        /// </summary>
        public static string CleanFileName(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return "item_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            string clean = code.Trim();
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                clean = clean.Replace(c, '_');
            }
            return clean.Replace(" ", "_");
        }

        /// <summary>
        /// ضغط وتصغير الصورة وحفظها محلياً وبالمسارات المستهدفة بأعلى كفاءة وأقل حجم باقة.
        /// يعيد الرابط النسبي المتوافق مع المتجر: images/products/prod_{code}_{slot}.jpg
        /// </summary>
        public static string SaveAndCompressImage(Image sourceImage, string productCode, int slot)
        {
            if (sourceImage == null) return null;

            int maxSize = slot == 1 ? Slot1MaxSize : Slot2MaxSize;
            byte[] jpegBytes = CompressToJpegBytes(sourceImage, maxSize, maxSize, JpegQuality);

            string safeCode = CleanFileName(productCode);
            string fileName = $"prod_{safeCode}_{slot}.jpg";
            string relativePath = $"images/products/{fileName}";

            var targetDirs = GetTargetDirectories();
            foreach (var dir in targetDirs)
            {
                try
                {
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                    string fullPath = Path.Combine(dir, fileName);
                    File.WriteAllBytes(fullPath, jpegBytes);
                }
                catch (Exception ex)
                {
                    AppLogger.Warn($"فشل حفظ صورة الصنف في {dir}: {ex.Message}", "SaveAndCompressImage");
                }
            }

            return relativePath;
        }

        /// <summary>
        /// حفظ ومعالجة صورة من مسار ملف محلي
        /// </summary>
        public static string SaveAndCompressFromFile(string filePath, string productCode, int slot)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath)) return null;

            using (var stream = new MemoryStream(File.ReadAllBytes(filePath)))
            using (var img = Image.FromStream(stream))
            {
                return SaveAndCompressImage(img, productCode, slot);
            }
        }

        /// <summary>
        /// ضغط الصورة وإعادة تحجيمها مع الحفاظ التام على النسبة والأبعاد (Aspect Ratio) وجودة واضحة بحجم 25-35 KB
        /// </summary>
        public static byte[] CompressToJpegBytes(Image source, int maxWidth, int maxHeight, long quality = 75L)
        {
            int originalWidth = source.Width;
            int originalHeight = source.Height;

            float ratioX = (float)maxWidth / originalWidth;
            float ratioY = (float)maxHeight / originalHeight;
            float ratio = Math.Min(ratioX, ratioY);

            int targetWidth = originalWidth;
            int targetHeight = originalHeight;

            // نقوم بالتصغير فقط إذا كانت الصورة أكبر من الحد الأقصى
            if (ratio < 1.0f)
            {
                targetWidth = Math.Max(1, (int)(originalWidth * ratio));
                targetHeight = Math.Max(1, (int)(originalHeight * ratio));
            }

            using (var bmp = new Bitmap(targetWidth, targetHeight, PixelFormat.Format24bppRgb))
            {
                bmp.SetResolution(source.HorizontalResolution > 0 ? source.HorizontalResolution : 96,
                                  source.VerticalResolution > 0 ? source.VerticalResolution : 96);

                using (var g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.White);
                    g.CompositingQuality = CompositingQuality.HighQuality;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                    using (var wrapMode = new ImageAttributes())
                    {
                        wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                        g.DrawImage(source, new Rectangle(0, 0, targetWidth, targetHeight), 0, 0, originalWidth, originalHeight, GraphicsUnit.Pixel, wrapMode);
                    }
                }

                ImageCodecInfo jpgEncoder = GetEncoder(ImageFormat.Jpeg);
                if (jpgEncoder == null)
                {
                    using (var ms = new MemoryStream())
                    {
                        bmp.Save(ms, ImageFormat.Jpeg);
                        return ms.ToArray();
                    }
                }

                var encoderParameters = new EncoderParameters(1);
                encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, quality);

                using (var ms = new MemoryStream())
                {
                    bmp.Save(ms, jpgEncoder, encoderParameters);
                    return ms.ToArray();
                }
            }
        }

        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }

        /// <summary>
        /// تحميل الصورة بشكل آمن دون إقفال الملف من القرص المحلي أو من رابط URL
        /// </summary>
        public static Image LoadImageSafe(string pathOrUrl)
        {
            if (string.IsNullOrWhiteSpace(pathOrUrl)) return null;

            pathOrUrl = pathOrUrl.Trim();

            // إذا كان رابط إنترنت
            if (pathOrUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                pathOrUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(8) })
                    {
                        byte[] bytes = client.GetByteArrayAsync(pathOrUrl).GetAwaiter().GetResult();
                        return Image.FromStream(new MemoryStream(bytes));
                    }
                }
                catch
                {
                    return null;
                }
            }

            // إذا كان مساراً محلياً نسبياً أو كاملاً
            string fullPath = ResolveFullPath(pathOrUrl);
            if (!string.IsNullOrEmpty(fullPath) && File.Exists(fullPath))
            {
                try
                {
                    byte[] bytes = File.ReadAllBytes(fullPath);
                    return Image.FromStream(new MemoryStream(bytes));
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }

        /// <summary>
        /// إيجاد المسار الكامل لملف صورة محلي بناءً على الرابط النسبي
        /// </summary>
        public static string ResolveFullPath(string relativeOrFullPath)
        {
            if (string.IsNullOrWhiteSpace(relativeOrFullPath)) return null;

            if (File.Exists(relativeOrFullPath)) return relativeOrFullPath;

            string normalized = relativeOrFullPath.Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);

            foreach (var dir in GetTargetDirectories())
            {
                // إذا كان المسار يبدأ بـ images\products
                string fileName = Path.GetFileName(normalized);
                string test1 = Path.Combine(dir, fileName);
                if (File.Exists(test1)) return test1;

                // فحص بمسار الأب
                string parentDir = Directory.GetParent(dir)?.FullName;
                if (!string.IsNullOrEmpty(parentDir))
                {
                    string test2 = Path.Combine(parentDir, normalized);
                    if (File.Exists(test2)) return test2;
                }
            }

            return null;
        }

        /// <summary>
        /// إرجاع حجم ملف الصورة بالكيلوبايت للعرض في الواجهة
        /// </summary>
        public static string GetFileSizeString(string pathOrUrl)
        {
            if (string.IsNullOrWhiteSpace(pathOrUrl)) return "";

            string full = ResolveFullPath(pathOrUrl);
            if (!string.IsNullOrEmpty(full) && File.Exists(full))
            {
                long bytes = new FileInfo(full).Length;
                return $"{(bytes / 1024.0):0.#} KB";
            }

            return "";
        }

        /// <summary>
        /// حذف ملف الصورة من المجلدات المحلية
        /// </summary>
        public static void DeleteProductImage(string relativeOrFullPath)
        {
            if (string.IsNullOrWhiteSpace(relativeOrFullPath)) return;

            string fileName = Path.GetFileName(relativeOrFullPath);
            if (string.IsNullOrWhiteSpace(fileName)) return;

            foreach (var dir in GetTargetDirectories())
            {
                try
                {
                    string test = Path.Combine(dir, fileName);
                    if (File.Exists(test)) File.Delete(test);
                }
                catch { }
            }
        }
    }
}

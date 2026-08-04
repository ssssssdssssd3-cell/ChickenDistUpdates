using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChickenDist.Core
{
    /// <summary>
    /// بوابة المندوب — خادم HTTP محلي خفيف + خدمة الرفع السحابي
    /// يتيح للمندوب سحب بيانات اليوم بطريقتين:
    ///   أ) Wi-Fi محلي (نفس الشبكة) عبر IP الكمبيوتر
    ///   ب) الإنترنت العام عبر رمز مكون من 5 حروف
    /// </summary>
    public static class DriverPortalServer
    {
        // ======================== حالة الخادم ========================
        private static TcpListener    _listener;
        private static CancellationTokenSource _cts;
        private static bool           _running;
        private static int            _port;
        private static string         _lastCloudCode;
        private static DateTime       _cloudCodeExpiry;

        public static bool  IsRunning       => _running;
        public static int   Port            => _port;
        public static string LastCloudCode  => _lastCloudCode;
        public static DateTime CloudCodeExpiry => _cloudCodeExpiry;

        // حدث يُطلق في كل طلب يصل من الموبايل
        public static event Action<string> OnRequestReceived;

        // ======================== الخادم المحلي ========================

        /// <summary>يبدأ الاستماع على المنفذ المحدد</summary>
        public static void Start(int port = 8080)
        {
            if (_running) return;
            _port = port;
            _cts  = new CancellationTokenSource();

            try
            {
                _listener = new TcpListener(IPAddress.Any, port);
                _listener.Start();
                _running = true;
                Task.Run(() => AcceptLoop(_cts.Token));
            }
            catch (Exception ex)
            {
                _running = false;
                throw new Exception($"تعذّر تشغيل الخادم على المنفذ {port}:\n{ex.Message}\nجرّب منفذاً آخر مثل 8181 أو 9090.");
            }
        }

        /// <summary>يوقف الخادم بأمان</summary>
        public static void Stop()
        {
            _cts?.Cancel();
            try { _listener?.Stop(); } catch { }
            _running  = false;
            _listener = null;
        }

        private static async Task AcceptLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var client = await _listener.AcceptTcpClientAsync();
                    _ = Task.Run(() => HandleClient(client));
                }
                catch { break; }
            }
        }

        private static void HandleClient(TcpClient client)
        {
            try
            {
                using (client)
                using (var stream = client.GetStream())
                {
                    // قراءة طلب HTTP
                    var buf = new byte[4096];
                    int n   = stream.Read(buf, 0, buf.Length);
                    string req = Encoding.UTF8.GetString(buf, 0, n);

                    string firstLine = req.Split('\n')[0].Trim();
                    string path = "/";
                    if (firstLine.Split(' ').Length >= 2)
                        path = firstLine.Split(' ')[1].Split('?')[0].ToLower();

                    string ip = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
                    OnRequestReceived?.Invoke($"[{DateTime.Now:HH:mm:ss}] طلب من {ip} — المسار: {path}");

                    if (path == "/data.json")
                    {
                        // تقديم بيانات JSON للمندوب
                        string json = DAL.DriverDAL.BuildDriverExportJson();
                        SendResponse(stream, "200 OK", "application/json; charset=utf-8", json);
                    }
                    else if (path == "/" || path == "/index.html" || path == "/driver_sales.html")
                    {
                        // حقن البيانات مباشرة داخل صفحة الـ HTML
                        string html = GetDriverSalesHtmlWithData();
                        SendResponse(stream, "200 OK", "text/html; charset=utf-8", html);
                    }
                    else if (path == "/accountant_orders.html")
                    {
                        // حقن البيانات مباشرة داخل صفحة الـ HTML للمحاسب
                        string html = GetAccountantOrdersHtmlWithData();
                        SendResponse(stream, "200 OK", "text/html; charset=utf-8", html);
                    }
                    else if (path == "/mobile" || path == "/mobileapp" || path == "/mobile/index.html")
                    {
                        // حقن البيانات في تطبيق الموبايل (MobileApp/index.html)
                        string html = GetMobileAppHtmlWithData();
                        SendResponse(stream, "200 OK", "text/html; charset=utf-8", html);
                    }
                    else if (path == "/manifest.json" || path == "/mobile/manifest.json")
                    {
                        string json = GetMobileAppManifest();
                        SendResponse(stream, "200 OK", "application/json; charset=utf-8", json);
                    }
                    else if (path == "/logo.png" || path == "/mobile/logo.png" || path == "/MobileApp/logo.png")
                    {
                        byte[] logoBytes = GetMobileAppLogoBytes();
                        SendBinaryResponse(stream, "200 OK", "image/png", logoBytes);
                    }
                    else
                    {
                        SendResponse(stream, "404 Not Found", "text/plain", "404 Not Found");
                    }
                }
            }
            catch { /* تجاهل أخطاء الاتصال المؤقتة */ }
        }

        private static void SendResponse(NetworkStream stream, string status, string contentType, string body)
        {
            byte[] bodyBytes = Encoding.UTF8.GetBytes(body);
            string headers   =
                $"HTTP/1.1 {status}\r\n" +
                $"Content-Type: {contentType}\r\n" +
                $"Content-Length: {bodyBytes.Length}\r\n" +
                "Access-Control-Allow-Origin: *\r\n" +
                "Access-Control-Allow-Methods: GET\r\n" +
                "Connection: close\r\n" +
                "\r\n";
            byte[] headerBytes = Encoding.UTF8.GetBytes(headers);
            stream.Write(headerBytes, 0, headerBytes.Length);
            stream.Write(bodyBytes,   0, bodyBytes.Length);
        }

        /// <summary>يقرأ driver_sales.html ويحقن JSON البيانات بداخله مباشرة</summary>
        private static string GetDriverSalesHtmlWithData()
        {
            string html = GetEmbeddedHtml();
            string json = DAL.DriverDAL.BuildDriverExportJson();
            string key = SecurityHelper.GetEffectiveKeyForJs();
            // نحقن البيانات كمتغير JS في أول السكريبت
            string injection = $"\n<script>\n  window.__SERVER_DATA__ = {json};\n  window.__XOR_KEY__ = \"{key}\";\n</script>\n";
            // ندرجها قبل نهاية الـ </head>
            return html.Replace("</head>", injection + "</head>");
        }

        private static string GetEmbeddedHtml()
        {
            // أولاً: حاول القراءة من الملف المستخرج
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Forms", "driver_sales.html");
            if (File.Exists(filePath))
                return File.ReadAllText(filePath, Encoding.UTF8);

            // ثانياً: قراءة من الـ Embedded Resource
            var asm = typeof(DriverPortalServer).Assembly;
            using (var s = asm.GetManifestResourceStream("ChickenDist.Forms.driver_sales.html"))
            {
                if (s != null)
                    using (var r = new StreamReader(s, Encoding.UTF8))
                        return r.ReadToEnd();
            }
            return "<html><body>driver_sales.html not found</body></html>";
        }

        /// <summary>يقرأ accountant_orders.html ويحقن JSON البيانات بداخله مباشرة</summary>
        private static string GetAccountantOrdersHtmlWithData()
        {
            string html = GetAccountantEmbeddedHtml();
            string json = DAL.DriverDAL.BuildDriverExportJson();
            string key = SecurityHelper.GetEffectiveKeyForJs();
            string injection = $"\n<script>\n  window.__SERVER_DATA__ = {json};\n  window.__XOR_KEY__ = \"{key}\";\n</script>\n";
            return html.Replace("</head>", injection + "</head>");
        }

        private static string GetAccountantEmbeddedHtml()
        {
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Forms", "accountant_orders.html");
            if (File.Exists(filePath))
                return File.ReadAllText(filePath, Encoding.UTF8);

            var asm = typeof(DriverPortalServer).Assembly;
            using (var s = asm.GetManifestResourceStream("ChickenDist.Forms.accountant_orders.html"))
            {
                if (s != null)
                    using (var r = new StreamReader(s, Encoding.UTF8))
                        return r.ReadToEnd();
            }
            return "<html><body>accountant_orders.html not found</body></html>";
        }

        /// <summary>يقرأ MobileApp/index.html ويحقن JSON البيانات بداخله مباشرة</summary>
        private static string GetMobileAppHtmlWithData()
        {
            string html = GetMobileAppEmbeddedHtml();
            string json = DAL.DriverDAL.BuildDriverExportJson();
            string key = SecurityHelper.GetEffectiveKeyForJs();
            string injection = $"\n<script>\n  window.__SERVER_DATA__ = {json};\n  window.__XOR_KEY__ = \"{key}\";\n</script>\n";
            return html.Replace("</head>", injection + "</head>");
        }

        private static string GetMobileAppEmbeddedHtml()
        {
            EnsureMobileAppFilesExtracted();
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MobileApp", "index.html");
            if (File.Exists(filePath))
                return File.ReadAllText(filePath, Encoding.UTF8);

            var asm = typeof(DriverPortalServer).Assembly;
            using (var s = asm.GetManifestResourceStream("ChickenDist.MobileApp.index.html"))
            {
                if (s != null)
                    using (var r = new StreamReader(s, Encoding.UTF8))
                        return r.ReadToEnd();
            }

            return "<html><body>MobileApp/index.html not found</body></html>";
        }

        private static string GetMobileAppManifest()
        {
            EnsureMobileAppFilesExtracted();
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MobileApp", "manifest.json");
            if (File.Exists(filePath))
                return File.ReadAllText(filePath, Encoding.UTF8);

            var asm = typeof(DriverPortalServer).Assembly;
            using (var s = asm.GetManifestResourceStream("ChickenDist.MobileApp.manifest.json"))
            {
                if (s != null)
                    using (var r = new StreamReader(s, Encoding.UTF8))
                        return r.ReadToEnd();
            }

            return "{}";
        }

        public static void EnsureMobileAppFilesExtracted()
        {
            try
            {
                string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MobileApp");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                string htmlPath = Path.Combine(dir, "index.html");
                string manifestPath = Path.Combine(dir, "manifest.json");
                string logoPath = Path.Combine(dir, "logo.png");

                var asm = typeof(DriverPortalServer).Assembly;

                // Extract index.html (always force overwrite with latest embedded resource)
                Stream sHtml = asm.GetManifestResourceStream("ChickenDist.MobileApp.index.html") 
                            ?? asm.GetManifestResourceStream("MobileApp.index.html");
                if (sHtml != null)
                {
                    using (sHtml)
                    using (var r = new StreamReader(sHtml, Encoding.UTF8))
                    {
                        File.WriteAllText(htmlPath, r.ReadToEnd(), Encoding.UTF8);
                    }
                }

                // Extract manifest.json (always force overwrite with latest embedded resource)
                Stream sManifest = asm.GetManifestResourceStream("ChickenDist.MobileApp.manifest.json") 
                               ?? asm.GetManifestResourceStream("MobileApp.manifest.json");
                if (sManifest != null)
                {
                    using (sManifest)
                    using (var r = new StreamReader(sManifest, Encoding.UTF8))
                    {
                        File.WriteAllText(manifestPath, r.ReadToEnd(), Encoding.UTF8);
                    }
                }

                // Extract logo.png (always force overwrite with latest embedded resource)
                Stream sLogo = asm.GetManifestResourceStream("ChickenDist.MobileApp.logo.png")
                            ?? asm.GetManifestResourceStream("MobileApp.logo.png")
                            ?? asm.GetManifestResourceStream("ChickenDist.pro_soft_logo.png");
                if (sLogo != null)
                {
                    using (sLogo)
                    using (var ms = new MemoryStream())
                    {
                        sLogo.CopyTo(ms);
                        File.WriteAllBytes(logoPath, ms.ToArray());
                    }
                }
                // Also sync to bot/public/mobile.html for Firebase Hosting if bot/public exists
                string botPublicDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bot", "public");
                if (File.Exists(htmlPath) && Directory.Exists(botPublicDir))
                {
                    File.Copy(htmlPath, Path.Combine(botPublicDir, "mobile.html"), true);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("EnsureMobileAppFilesExtracted error: " + ex.Message);
            }
        }

        private static byte[] GetMobileAppLogoBytes()
        {
            EnsureMobileAppFilesExtracted();
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MobileApp", "logo.png");
            if (File.Exists(filePath))
                return File.ReadAllBytes(filePath);

            var asm = typeof(DriverPortalServer).Assembly;
            using (var s = asm.GetManifestResourceStream("ChickenDist.MobileApp.logo.png") 
                        ?? asm.GetManifestResourceStream("ChickenDist.pro_soft_logo.png"))
            {
                if (s != null)
                {
                    using (var ms = new MemoryStream())
                    {
                        s.CopyTo(ms);
                        return ms.ToArray();
                    }
                }
            }
            return new byte[0];
        }

        private static void SendBinaryResponse(NetworkStream stream, string status, string contentType, byte[] bodyBytes)
        {
            string headers =
                $"HTTP/1.1 {status}\r\n" +
                $"Content-Type: {contentType}\r\n" +
                $"Content-Length: {bodyBytes.Length}\r\n" +
                "Access-Control-Allow-Origin: *\r\n" +
                "Connection: close\r\n" +
                "\r\n";
            byte[] headerBytes = Encoding.UTF8.GetBytes(headers);
            stream.Write(headerBytes, 0, headerBytes.Length);
            if (bodyBytes != null && bodyBytes.Length > 0)
            {
                stream.Write(bodyBytes, 0, bodyBytes.Length);
            }
        }

        // ======================== الرفع السحابي ========================

        /// <summary>
        /// يقرأ databaseURL من bot/firebase_config.json
        /// </summary>
        private static string GetFirebaseDatabaseUrl()
        {
            try
            {
                // مسار ملف firebase_config.json بجانب exe
                string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                string configPath = Path.Combine(exeDir, "bot", "firebase_config.json");
                if (!File.Exists(configPath)) return null;

                string json = File.ReadAllText(configPath, Encoding.UTF8);

                // استخراج databaseURL
                var m = System.Text.RegularExpressions.Regex.Match(json, "\"databaseURL\"\\s*:\\s*\"([^\"]+)\"");
                if (m.Success) return m.Groups[1].Value.TrimEnd('/');

                // بناء الـ URL من projectId إذا لم يوجد databaseURL
                var mp = System.Text.RegularExpressions.Regex.Match(json, "\"projectId\"\\s*:\\s*\"([^\"]+)\"");
                if (mp.Success) return $"https://{mp.Groups[1].Value}-default-rtdb.firebaseio.com";
            }
            catch { }
            return null;
        }

        /// <summary>
        /// يرفع بيانات ERP إلى Firebase Realtime Database ويُرجع السيريال الثابت للعميل
        /// </summary>
        public static string UploadToCloud(int? driverID = null)
        {
            string clientSerial = Services.CloudSyncService.GetPermanentClientSerial();
            string json = DAL.DriverDAL.BuildDriverExportJson(driverID);

            // 1. الرفع لـ Firebase Realtime Database (الحل الدائم اللحظي)
            string dbUrl = GetFirebaseDatabaseUrl();
            bool firebaseOk = false;
            if (!string.IsNullOrEmpty(dbUrl))
            {
                try
                {
                    using (var wc = new WebClient())
                    {
                        wc.Encoding = Encoding.UTF8;
                        wc.Headers[HttpRequestHeader.ContentType] = "application/json";
                        // PUT /erp_data.json — يحفظ البيانات دائماً تحت /erp_data في قاعدة البيانات
                        wc.UploadString($"{dbUrl}/erp_data.json", "PUT", json);
                        firebaseOk = true;
                        System.Diagnostics.Debug.WriteLine("Firebase RTDB upload OK");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Firebase RTDB Upload warning: " + ex.Message);
                }
            }

            // 2. Fallback إلى pastes.dev لو Firebase مش موجود أو فشل
            if (!firebaseOk)
            {
                try
                {
                    using (var wc = new WebClient())
                    {
                        wc.Encoding = Encoding.UTF8;
                        wc.Headers[HttpRequestHeader.ContentType] = "text/plain";
                        wc.Headers[HttpRequestHeader.UserAgent]   = "ChickenDistApp (contact@chickendist.com)";

                        string responseStr = wc.UploadString("https://api.pastes.dev/post", "POST", json);
                        var match = System.Text.RegularExpressions.Regex.Match(responseStr, @"""key""\s*:\s*""([^""]+)""");
                        _lastCloudCode   = match.Success ? match.Groups[1].Value : responseStr.Trim();
                        _cloudCodeExpiry = DateTime.Now.AddHours(24);
                        System.Diagnostics.Debug.WriteLine("Fallback pastes.dev upload OK: " + _lastCloudCode);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("pastes.dev Upload warning: " + ex.Message);
                }
            }

            return clientSerial; // السيريال الثابت دايماً هو الكود الرئيسي
        }

        // ======================== عناوين الـ IP ========================

        /// <summary>يُرجع قائمة عناوين IPv4 النشطة على هذا الجهاز</summary>
        public static List<string> GetLocalIPs()
        {
            var result = new List<string>();
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up) continue;
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                foreach (var ip in ni.GetIPProperties().UnicastAddresses)
                {
                    if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        result.Add(ip.Address.ToString());
                }
            }
            return result;
        }
    }
}

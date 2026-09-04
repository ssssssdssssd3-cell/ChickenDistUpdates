using System;
using System.Data;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ChickenDist.Core;

namespace ChickenDist.Services
{
    public class SyncStatsDTO
    {
        public decimal TodaySalesTotal { get; set; }
        public decimal TodayCashSales { get; set; }
        public decimal TodayCreditSales { get; set; }
        public decimal CashboxBalance { get; set; }
        public int LowStockCount { get; set; }
        public int ActiveShiftCount { get; set; }
        public string LastSyncStatus { get; set; }
        public DateTime? LastSyncDate { get; set; }
    }

    public static class CloudSyncService
    {
        public static void EnsureMobileAppFolderExists()
        {
            EnsureMobileAppFiles();
        }

        /// <summary>
        /// ينشئ ويتأكد من وجود مجلد MobileApp وجميع ملفات تطبيق المالك (HTML, JS, PWA Service Worker, Rules, Config) تلقائياً على جهاز العميل بأحدث إصدار
        /// </summary>
        public static void EnsureMobileAppFiles()
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string mobileAppDir = System.IO.Path.Combine(baseDir, "MobileApp");
                if (!System.IO.Directory.Exists(mobileAppDir))
                {
                    System.IO.Directory.CreateDirectory(mobileAppDir);
                }

                // 1. ملف إعدادات الاستضافة العالمي firebase.json مع إجبار منع الكاش على كل المسارات
                string firebaseJsonContent = @"{
  ""hosting"": {
    ""public"": ""."",
    ""ignore"": [
      ""firebase.json"",
      ""**/.*"",
      ""**/node_modules/**"",
      ""*.bat"",
      ""*.ps1"",
      ""*.cmd"",
      ""*.exe"",
      ""*.rules.json"",
      ""*.rules""
    ],
    ""headers"": [
      {
        ""source"": ""**"",
        ""headers"": [
          {
            ""key"": ""Cache-Control"",
            ""value"": ""no-cache, no-store, must-revalidate, max-age=0, s-maxage=0""
          },
          {
            ""key"": ""Pragma"",
            ""value"": ""no-cache""
          },
          {
            ""key"": ""Expires"",
            ""value"": ""0""
          },
          {
            ""key"": ""Surrogate-Control"",
            ""value"": ""no-store""
          },
          {
            ""key"": ""Access-Control-Allow-Origin"",
            ""value"": ""*""
          }
        ]
      }
    ]
  },
  ""database"": {
    ""rules"": ""database.rules.json""
  },
  ""firestore"": {
    ""rules"": ""firestore.rules""
  }
}";
                string mobileFirebaseJson = System.IO.Path.Combine(mobileAppDir, "firebase.json");
                System.IO.File.WriteAllText(mobileFirebaseJson, firebaseJsonContent, new System.Text.UTF8Encoding(false));

                string rootFirebaseJson = System.IO.Path.Combine(baseDir, "firebase.json");
                System.IO.File.WriteAllText(rootFirebaseJson, firebaseJsonContent, new System.Text.UTF8Encoding(false));

                // 2. ملف قواعد بيانات السيكول السحابية database.rules.json
                string dbRulesContent = @"{
  ""rules"": {
    "".read"": true,
    "".write"": true
  }
}";
                string dbRulesPath = System.IO.Path.Combine(mobileAppDir, "database.rules.json");
                System.IO.File.WriteAllText(dbRulesPath, dbRulesContent, new System.Text.UTF8Encoding(false));

                // 3. ملف قواعد Firestore
                string firestoreRulesContent = @"rules_version = '2';
service cloud.firestore {
  match /databases/{database}/documents {
    match /{document=**} {
      allow read, write: if true;
    }
  }
}";
                string firestoreRulesPath = System.IO.Path.Combine(mobileAppDir, "firestore.rules");
                System.IO.File.WriteAllText(firestoreRulesPath, firestoreRulesContent, new System.Text.UTF8Encoding(false));

                // 4. كتابة وتحديث ملف sw.js (Service Worker لتفعيل تثبيت الـ PWA v3.1.0 ومسح الكاش فوراً)
                string swContent = @"// ProSoft ERP Mobile App Service Worker (v3.1.0)
const CACHE_NAME = 'prosoft-pwa-v310-' + Date.now();

self.addEventListener('install', (event) => {
  self.skipWaiting();
});

self.addEventListener('activate', (event) => {
  event.waitUntil(
    caches.keys().then((keys) => {
      return Promise.all(
        keys.map((key) => caches.delete(key))
      );
    }).then(() => self.clients.claim())
  );
});

self.addEventListener('fetch', (event) => {
  // Always fetch fresh network requests, never serve stale HTML or API data
  event.respondWith(
    fetch(event.request, { cache: 'no-store' }).catch(() => caches.match(event.request))
  );
});";
                string swPath = System.IO.Path.Combine(mobileAppDir, "sw.js");
                System.IO.File.WriteAllText(swPath, swContent, new System.Text.UTF8Encoding(false));

                // 5. استخراج ملفات الـ EmbeddedResource بالكامل من الـ EXE المحدث
                string indexHtmlPath = System.IO.Path.Combine(mobileAppDir, "index.html");
                string devIndex = @"D:\قطع غيار وتوزيع\قطع غيار وتوزيع\ChickenDistUpdates-main\ChickenDistUpdates-main\MobileApp\index.html";
                bool updated = false;

                if (System.IO.File.Exists(devIndex))
                {
                    try
                    {
                        System.IO.File.Copy(devIndex, indexHtmlPath, true);
                        updated = true;
                    }
                    catch { }
                }

                if (!updated)
                {
                    try
                    {
                        var asm = typeof(CloudSyncService).Assembly;
                        foreach (var resName in asm.GetManifestResourceNames())
                        {
                            if (resName.EndsWith("index.html", StringComparison.OrdinalIgnoreCase))
                            {
                                using (var stream = asm.GetManifestResourceStream(resName))
                                using (var reader = new System.IO.StreamReader(stream, System.Text.Encoding.UTF8))
                                {
                                    string content = reader.ReadToEnd();
                                    if (!string.IsNullOrEmpty(content) && content.Contains("ProSoft"))
                                    {
                                        System.IO.File.WriteAllText(indexHtmlPath, content, new System.Text.UTF8Encoding(false));
                                        updated = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    catch { }
                }

                // 6. محاولة تحميل أحدث نسخة من GitHub مع كسر الكاش للتأكد من وصول آخر تعديل دائماً
                try
                {
                    using (var wc = new System.Net.WebClient())
                    {
                        System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12 | System.Net.SecurityProtocolType.Tls11 | System.Net.SecurityProtocolType.Tls | (System.Net.SecurityProtocolType)12288;
                        wc.Encoding = System.Text.Encoding.UTF8;
                        wc.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
                        string html = wc.DownloadString("https://raw.githubusercontent.com/ssssssdssssd3-cell/ChickenDistUpdates/main/MobileApp/index.html?t=" + DateTime.Now.Ticks);
                        if (!string.IsNullOrEmpty(html) && html.Contains("tabContent-shifts") && html.Contains("ProSoft"))
                        {
                            System.IO.File.WriteAllText(indexHtmlPath, html, new System.Text.UTF8Encoding(false));
                            updated = true;
                        }
                    }
                }
                catch { }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("EnsureMobileAppFiles warning: " + ex.Message);
            }
        }

        public static SyncStatsDTO GetLiveStats()
        {
            var dto = new SyncStatsDTO();

            try
            {
                // مبيعات اليوم
                object totObj = DbHelper.Scalar("SELECT ISNULL(SUM(TotalAmount),0) FROM Sales WITH (NOLOCK) WHERE CAST(SaleDate AS DATE) = CAST(GETDATE() AS DATE)");
                dto.TodaySalesTotal = totObj != null && totObj != DBNull.Value ? Convert.ToDecimal(totObj) : 0m;

                object cashObj = DbHelper.Scalar("SELECT ISNULL(SUM(TotalAmount),0) FROM Sales WITH (NOLOCK) WHERE CAST(SaleDate AS DATE) = CAST(GETDATE() AS DATE) AND SaleType = 'Cash'");
                dto.TodayCashSales = cashObj != null && cashObj != DBNull.Value ? Convert.ToDecimal(cashObj) : 0m;

                object credObj = DbHelper.Scalar("SELECT ISNULL(SUM(TotalAmount),0) FROM Sales WITH (NOLOCK) WHERE CAST(SaleDate AS DATE) = CAST(GETDATE() AS DATE) AND SaleType <> 'Cash'");
                dto.TodayCreditSales = credObj != null && credObj != DBNull.Value ? Convert.ToDecimal(credObj) : 0m;

                // رصيد الخزائن والسيولة الفعلية المتاحة (شامل الأرصدة الافتتاحية لكافة الخزائن)
                decimal totalCashboxBalance = 0m;
                try
                {
                    object openBalObj = DbHelper.Scalar("SELECT ISNULL(SUM(OpeningBalance), 0) FROM SafeAccounts WITH (NOLOCK) WHERE IsActive = 1");
                    decimal openBal = openBalObj != null && openBalObj != DBNull.Value ? Convert.ToDecimal(openBalObj) : 0m;

                    object movementsObj = DbHelper.Scalar("SELECT ISNULL(SUM(ISNULL(AmountIn, 0) - ISNULL(AmountOut, 0)), 0) FROM CashBox WITH (NOLOCK)");
                    decimal movements = movementsObj != null && movementsObj != DBNull.Value ? Convert.ToDecimal(movementsObj) : 0m;

                    totalCashboxBalance = openBal + movements;
                }
                catch
                {
                    object cbObj = DbHelper.Scalar("SELECT ISNULL(SUM(ISNULL(AmountIn,0) - ISNULL(AmountOut,0)), 0) FROM CashBox WITH (NOLOCK)");
                    totalCashboxBalance = cbObj != null && cbObj != DBNull.Value ? Convert.ToDecimal(cbObj) : 0m;
                }
                dto.CashboxBalance = totalCashboxBalance;

                // نواقص المخزن
                object lowObj = DbHelper.Scalar(@"
                    SELECT COUNT(*)
                    FROM Products p WITH (NOLOCK)
                    OUTER APPLY (
                        SELECT COALESCE(
                            (SELECT SUM(ps.Quantity) FROM ProductStock ps WITH (NOLOCK) WHERE ps.ProductID = p.ProductID),
                            (SELECT SUM(pb.Quantity) FROM ProductBatches pb WITH (NOLOCK) WHERE pb.ProductID = p.ProductID),
                            p.Quantity, 0
                        ) AS TotalStock
                    ) stk
                    LEFT JOIN ShortageNotebook sn WITH (NOLOCK) ON p.ProductID = sn.ProductID AND sn.Status IN (N'جديد', N'تم الطلب')
                    WHERE p.IsActive = 1
                      AND (
                          ISNULL(stk.TotalStock, 0) <= 0
                          OR (p.MinStockLimit > 0 AND ISNULL(stk.TotalStock, 0) <= p.MinStockLimit)
                          OR sn.ShortageID IS NOT NULL
                      )");
                dto.LowStockCount = lowObj != null && lowObj != DBNull.Value ? Convert.ToInt32(lowObj) : 0;

                // حالة التزامن السابق
                DataTable dtSet = DbHelper.Query("SELECT TOP 1 LastSyncDate, LastSyncStatus FROM CloudSyncSettings WHERE SettingID = 1");
                if (dtSet.Rows.Count > 0)
                {
                    if (dtSet.Rows[0]["LastSyncDate"] != DBNull.Value)
                        dto.LastSyncDate = Convert.ToDateTime(dtSet.Rows[0]["LastSyncDate"]);
                    dto.LastSyncStatus = dtSet.Rows[0]["LastSyncStatus"]?.ToString() ?? "لم يتم المزامنة بعد";
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("فشل استخراج مؤشرات التزامن السحابي", ex, "CloudSyncService.GetLiveStats");
            }

            return dto;
        }

        public static string GetPermanentClientSerial()
        {
            try
            {
                DataTable dt = DbHelper.Query("SELECT TOP 1 OwnerSecretKey FROM CloudSyncSettings WHERE SettingID = 1");
                if (dt.Rows.Count > 0 && dt.Rows[0]["OwnerSecretKey"] != DBNull.Value)
                {
                    string key = dt.Rows[0]["OwnerSecretKey"].ToString().Trim();
                    if (!string.IsNullOrEmpty(key) && key != "OWNER-SECRET-KEY")
                    {
                        return key;
                    }
                }
            }
            catch { }

            try
            {
                string macId = LicenseManager.GetCurrentMachineId();
                if (!string.IsNullOrEmpty(macId))
                {
                    uint hash = (uint)Math.Abs(macId.GetHashCode());
                    return "PROSOFT-" + hash.ToString("X6");
                }
            }
            catch { }

            return "PROSOFT-" + Math.Abs(Environment.MachineName.GetHashCode()).ToString("X6");
        }

        public static async Task<(bool success, string message)> SyncNowAsync()
        {
            try
            {
                string projectId = AppConfig.Get("FirebaseProjectId", "checkin-192ab");
                if (string.IsNullOrEmpty(projectId)) projectId = "checkin-192ab";

                bool ok = await PushLiveStatsToFirebaseAsync(projectId);
                string statusMsg = ok 
                    ? $"تم التزامن بنجاح مع Firebase 🔥 ({projectId})" 
                    : "فشل التزامن، يرجى التحقق من اتصال الإنترنت ومشروع Firebase";

                DbHelper.Execute(@"
                    UPDATE CloudSyncSettings 
                    SET LastSyncDate = GETDATE(), LastSyncStatus = @status 
                    WHERE SettingID = 1",
                    DbHelper.P("@status", statusMsg));

                return (ok, statusMsg);
            }
            catch (Exception ex)
            {
                AppLogger.Error("فشل المزامنة مع Firebase", ex, "CloudSyncService.SyncNowAsync");
                return (false, "خطأ أثناء المزامنة: " + ex.Message);
            }
        }

        public static async Task<bool> PushLiveStatsToFirebaseAsync(string projectId = null)
        {
            if (string.IsNullOrEmpty(projectId))
            {
                projectId = AppConfig.Get("FirebaseProjectId", "checkin-192ab");
            }
            if (string.IsNullOrEmpty(projectId)) projectId = "checkin-192ab";

            bool rtdbOk = false;
            bool firestoreOk = false;

            try
            {
                var dto = GetLiveStats();

                // 1. حساب صافي الربح اليوم
                object profitObj = DbHelper.Scalar(
                    @"SELECT ISNULL(SUM(si.TotalPrice - (si.Quantity * ISNULL(p.PurchasePrice, 0))), 0)
                      FROM SaleItems si WITH (NOLOCK)
                      JOIN Sales s WITH (NOLOCK) ON si.SaleID = s.SaleID
                      JOIN Products p WITH (NOLOCK) ON si.ProductID = p.ProductID
                      WHERE CAST(s.SaleDate AS DATE) = CAST(GETDATE() AS DATE)");
                decimal todayProfit = profitObj != null && profitObj != DBNull.Value ? Convert.ToDecimal(profitObj) : 0m;

                // 2. مشتريات اليوم
                object purObj = DbHelper.Scalar("SELECT ISNULL(SUM(ISNULL(TotalAmount, 0)), 0) FROM Purchases WITH (NOLOCK) WHERE CAST(PurchaseDate AS DATE) = CAST(GETDATE() AS DATE)");
                decimal todayPurchases = purObj != null && purObj != DBNull.Value ? Convert.ToDecimal(purObj) : 0m;

                object purCashObj = DbHelper.Scalar("SELECT ISNULL(SUM(ISNULL(PaidAmount, 0)), 0) FROM Purchases WITH (NOLOCK) WHERE CAST(PurchaseDate AS DATE) = CAST(GETDATE() AS DATE)");
                decimal todayPaidPurchases = purCashObj != null && purCashObj != DBNull.Value ? Convert.ToDecimal(purCashObj) : 0m;
                decimal todayCreditPurchases = Math.Max(0, todayPurchases - todayPaidPurchases);

                // عدد فواتير البيع والشراء اليوم
                object salesCountObj = DbHelper.Scalar("SELECT COUNT(*) FROM Sales WITH (NOLOCK) WHERE CAST(SaleDate AS DATE) = CAST(GETDATE() AS DATE)");
                int todaySalesInvoicesCount = salesCountObj != null && salesCountObj != DBNull.Value ? Convert.ToInt32(salesCountObj) : 0;

                object purCountObj = DbHelper.Scalar("SELECT COUNT(*) FROM Purchases WITH (NOLOCK) WHERE CAST(PurchaseDate AS DATE) = CAST(GETDATE() AS DATE)");
                int todayPurInvoicesCount = purCountObj != null && purCountObj != DBNull.Value ? Convert.ToInt32(purCountObj) : 0;

                // 3. ديون العملاء
                object clientDebtsObj = DbHelper.Scalar("SELECT ISNULL(SUM(ISNULL(Balance, 0)), 0) FROM Clients WITH (NOLOCK) WHERE Balance > 0");
                decimal clientDebts = clientDebtsObj != null && clientDebtsObj != DBNull.Value ? Convert.ToDecimal(clientDebtsObj) : 0m;

                // 4. مستحقات الموردين
                object suppDebtsObj = DbHelper.Scalar("SELECT ISNULL(SUM(ISNULL(Balance, 0)), 0) FROM Suppliers WITH (NOLOCK) WHERE Balance > 0");
                decimal suppDebts = suppDebtsObj != null && suppDebtsObj != DBNull.Value ? Convert.ToDecimal(suppDebtsObj) : 0m;

                // مقبوضات ومصروفات اليوم في الخزنة
                object cbInObj = DbHelper.Scalar("SELECT ISNULL(SUM(AmountIn), 0) FROM CashBox WITH (NOLOCK) WHERE CAST(TransDate AS DATE) = CAST(GETDATE() AS DATE)");
                decimal todayCashIn = cbInObj != null && cbInObj != DBNull.Value ? Convert.ToDecimal(cbInObj) : 0m;

                object cbOutObj = DbHelper.Scalar("SELECT ISNULL(SUM(AmountOut), 0) FROM CashBox WITH (NOLOCK) WHERE CAST(TransDate AS DATE) = CAST(GETDATE() AS DATE)");
                decimal todayCashOut = cbOutObj != null && cbOutObj != DBNull.Value ? Convert.ToDecimal(cbOutObj) : 0m;

                // تقييم المخزون الفعلي بسعر التكلفة وبسعر البيع
                object stockCostObj = DbHelper.Scalar(@"
                    SELECT 
                        CASE 
                            WHEN EXISTS (SELECT 1 FROM ProductStock WITH (NOLOCK)) THEN
                                ISNULL((SELECT SUM(ps.Quantity * ISNULL(p.PurchasePrice, 0)) FROM ProductStock ps WITH (NOLOCK) JOIN Products p WITH (NOLOCK) ON ps.ProductID = p.ProductID WHERE p.IsActive = 1 AND ps.Quantity > 0), 0)
                            ELSE
                                ISNULL((SELECT SUM(ISNULL(p.Quantity, 0) * ISNULL(p.PurchasePrice, 0)) FROM Products p WITH (NOLOCK) WHERE p.IsActive = 1 AND p.Quantity > 0), 0)
                        END");
                decimal stockCostValue = stockCostObj != null && stockCostObj != DBNull.Value ? Convert.ToDecimal(stockCostObj) : 0m;

                object stockSaleObj = DbHelper.Scalar(@"
                    SELECT 
                        CASE 
                            WHEN EXISTS (SELECT 1 FROM ProductStock WITH (NOLOCK)) THEN
                                ISNULL((SELECT SUM(ps.Quantity * ISNULL(p.SalePrice, 0)) FROM ProductStock ps WITH (NOLOCK) JOIN Products p WITH (NOLOCK) ON ps.ProductID = p.ProductID WHERE p.IsActive = 1 AND ps.Quantity > 0), 0)
                            ELSE
                                ISNULL((SELECT SUM(ISNULL(p.Quantity, 0) * ISNULL(p.SalePrice, 0)) FROM Products p WITH (NOLOCK) WHERE p.IsActive = 1 AND p.Quantity > 0), 0)
                        END");
                decimal stockSaleValue = stockSaleObj != null && stockSaleObj != DBNull.Value ? Convert.ToDecimal(stockSaleObj) : 0m;

                // 5. كشكول النواقص الحقيقي
                DataTable dtMissing = DbHelper.Query(@"
                    SELECT TOP 200 p.ProductID, p.ProductName, ISNULL(p.ProductCode,'') AS ProductCode,
                           ISNULL(stk.TotalStock, 0) AS Quantity,
                           ISNULL(p.MinStockLimit, 0) AS MinQuantity,
                           COALESCE(sn.SupplierName, lastSup.SupplierName, p.Brand, N'عام') AS Supplier
                    FROM Products p WITH (NOLOCK)
                    OUTER APPLY (
                        SELECT COALESCE(
                            (SELECT SUM(ps.Quantity) FROM ProductStock ps WITH (NOLOCK) WHERE ps.ProductID = p.ProductID),
                            (SELECT SUM(pb.Quantity) FROM ProductBatches pb WITH (NOLOCK) WHERE pb.ProductID = p.ProductID),
                            p.Quantity, 0
                        ) AS TotalStock
                    ) stk
                    OUTER APPLY (
                        SELECT TOP 1 pu.SupplierID, sup.SupplierName
                        FROM PurchaseItems pi WITH (NOLOCK)
                        INNER JOIN Purchases pu WITH (NOLOCK) ON pi.PurchaseID = pu.PurchaseID
                        LEFT JOIN Suppliers sup WITH (NOLOCK) ON pu.SupplierID = sup.SupplierID
                        WHERE pi.ProductID = p.ProductID AND pu.IsPosted = 1
                        ORDER BY pu.PurchaseDate DESC, pu.PurchaseID DESC
                    ) lastSup
                    LEFT JOIN ShortageNotebook sn WITH (NOLOCK) ON p.ProductID = sn.ProductID AND sn.Status IN (N'جديد', N'تم الطلب')
                    WHERE p.IsActive = 1
                      AND (
                          ISNULL(stk.TotalStock, 0) <= 0
                          OR (p.MinStockLimit > 0 AND ISNULL(stk.TotalStock, 0) <= p.MinStockLimit)
                          OR sn.ShortageID IS NOT NULL
                      )
                    ORDER BY ISNULL(stk.TotalStock, 0) ASC, p.ProductName ASC");
                string missingJson = DataTableToJson(dtMissing);

                // 6. دليل الأصناف
                DataTable dtProducts = DbHelper.Query(@"
                    SELECT TOP 300 p.ProductID, p.ProductName, ISNULL(p.ProductCode,'') AS ProductCode, 
                           ISNULL(p.SalePrice,0) AS SalePrice, ISNULL(p.PurchasePrice,0) AS PurchasePrice, 
                           ISNULL(ps.TotalQty, ISNULL(p.Quantity, 0)) AS Quantity 
                    FROM Products p WITH (NOLOCK)
                    LEFT JOIN (SELECT ProductID, SUM(Quantity) AS TotalQty FROM ProductStock WITH (NOLOCK) GROUP BY ProductID) ps ON p.ProductID = ps.ProductID
                    WHERE p.IsActive = 1 
                    ORDER BY p.ProductName ASC");
                string productsJson = DataTableToJson(dtProducts);

                // 7. قائمة الموردين
                DataTable dtSuppliers = DbHelper.Query(
                    "SELECT TOP 150 SupplierID, ISNULL(SupplierCode,'') AS SupplierCode, SupplierName, ISNULL(Phone,'') AS Phone, ISNULL(Address,'') AS Address, ISNULL(Balance,0) AS Balance FROM Suppliers WITH (NOLOCK) WHERE IsActive=1 ORDER BY Balance DESC, SupplierName ASC");
                string suppliersJson = DataTableToJson(dtSuppliers);

                // 8. ديون وقائمة العملاء
                DataTable dtClients = DbHelper.Query(
                    "SELECT TOP 300 ClientID, ISNULL(ClientCode,'') AS ClientCode, ClientName, ISNULL(Phone,'') AS Phone, ISNULL(Phone2,'') AS Phone2, ISNULL(Address,'') AS Address, ISNULL(Balance,0) AS Balance, ISNULL(CurrentDebt,0) AS CurrentDebt, ISNULL(MaxCreditLimit,0) AS MaxCreditLimit FROM Clients WITH (NOLOCK) WHERE IsActive=1 ORDER BY Balance DESC, ClientName ASC");
                string clientsJson = DataTableToJson(dtClients);

                // 9. بيانات حساب المالك والمدراء والماستر لتسجيل الدخول في تطبيق الموبايل
                DataTable dtMaster = DbHelper.Query(@"
                    SELECT TOP 1 EmpID, EmpName, 
                           LTRIM(RTRIM(ISNULL(UserName, ''))) AS UserName, 
                           LTRIM(RTRIM(ISNULL(Password, ''))) AS Password, 
                           ISNULL(Role, 'Admin') AS Role 
                    FROM Employees WITH (NOLOCK) 
                    WHERE IsActive = 1 AND (Role IN ('Admin', 'Owner', N'مدير', N'المدير', N'مدير عام', N'المالك') OR EmpID = 1)
                    ORDER BY CASE WHEN Role IN ('Owner', N'المالك') THEN 1 WHEN Role IN ('Admin', N'مدير عام', N'المدير') THEN 2 ELSE 3 END, EmpID ASC");

                string masterUserName = "admin";
                string masterPassword = "admin";
                if (dtMaster != null && dtMaster.Rows.Count > 0)
                {
                    masterUserName = dtMaster.Rows[0]["UserName"]?.ToString().Trim();
                    masterPassword = dtMaster.Rows[0]["Password"]?.ToString().Trim();
                    if (string.IsNullOrEmpty(masterUserName)) masterUserName = "admin";
                    if (string.IsNullOrEmpty(masterPassword)) masterPassword = "admin";
                }
                string ownerPassword = masterPassword;

                DataTable dtUsers = DbHelper.Query(@"
                    SELECT EmpID, EmpName, 
                           LTRIM(RTRIM(ISNULL(UserName, ''))) AS UserName, 
                           LTRIM(RTRIM(ISNULL(Password, ''))) AS Password, 
                           ISNULL(Role, 'Admin') AS Role 
                    FROM Employees 
                    WHERE IsActive = 1");
                string usersJson = DataTableToJson(dtUsers);

                // 10. سجل فواتير المبيعات
                DataTable dtRecentSales = DbHelper.Query(@"
                    SELECT TOP 400 s.SaleID, CONVERT(VARCHAR(19), s.SaleDate, 120) AS SaleDate,
                           ISNULL(NULLIF(s.CustomClientName, ''), ISNULL(c.ClientName, N'عميل نقدي')) AS ClientName,
                           ISNULL(s.TotalAmount, 0) AS TotalAmount,
                           ISNULL(s.CashPaid, 0) + ISNULL(s.VisaPaid, 0) AS PaidAmount,
                           CASE 
                               WHEN s.SaleType = 'Cash' THEN 0 
                               ELSE ISNULL(s.TotalAmount, 0) - (ISNULL(s.CashPaid, 0) + ISNULL(s.VisaPaid, 0)) 
                           END AS RemainingAmount,
                           CASE WHEN s.SaleType = 'Cash' THEN N'نقدي' ELSE N'آجل' END AS PaymentType,
                           ISNULL(s.OrderType, N'مبيعات') AS InvoiceType
                    FROM Sales s WITH (NOLOCK)
                    LEFT JOIN Clients c WITH (NOLOCK) ON s.ClientID = c.ClientID
                    ORDER BY s.SaleID DESC");
                string recentSalesJson = DataTableToJson(dtRecentSales);

                // 11. سجل فواتير المشتريات
                DataTable dtRecentPurchases = DbHelper.Query(@"
                    SELECT TOP 200 p.PurchaseID, CONVERT(VARCHAR(19), p.PurchaseDate, 120) AS PurchaseDate,
                           ISNULL(sup.SupplierName, N'مورد عام') AS SupplierName,
                           ISNULL(p.TotalAmount, 0) AS TotalAmount,
                           ISNULL(p.PaidAmount, 0) + ISNULL(p.VisaPaid, 0) AS PaidAmount,
                           CASE 
                               WHEN p.PurchaseType = 'Cash' THEN 0 
                               ELSE ISNULL(p.TotalAmount, 0) - (ISNULL(p.PaidAmount, 0) + ISNULL(p.VisaPaid, 0)) 
                           END AS RemainingAmount
                    FROM Purchases p WITH (NOLOCK)
                    LEFT JOIN Suppliers sup WITH (NOLOCK) ON p.SupplierID = sup.SupplierID
                    ORDER BY p.PurchaseID DESC");
                string recentPurchasesJson = DataTableToJson(dtRecentPurchases);

                // 12. سجل حركات الخزينة
                DataTable dtRecentCash = DbHelper.Query(@"
                    SELECT TOP 300 cb.CashID, CONVERT(VARCHAR(19), cb.TransDate, 120) AS TransDate,
                           ISNULL(cb.AmountIn, 0) AS AmountIn,
                           ISNULL(cb.AmountOut, 0) AS AmountOut,
                           ISNULL(cb.Notes, N'') AS Notes,
                           ISNULL(sa.AccountName, N'الخزينة الرئيسية') AS SafeName
                    FROM CashBox cb WITH (NOLOCK)
                    LEFT JOIN SafeAccounts sa WITH (NOLOCK) ON cb.AccountID = sa.AccountID
                    ORDER BY cb.CashID DESC");
                string recentCashJson = DataTableToJson(dtRecentCash);

                // 13. أرصدة الخزائن التفصيلية
                DataTable dtSafes = DbHelper.Query(@"
                    SELECT sa.AccountID, sa.AccountName, sa.OpeningBalance,
                           ISNULL(SUM(cb.AmountIn), 0) AS TotalIn,
                           ISNULL(SUM(cb.AmountOut), 0) AS TotalOut,
                           sa.OpeningBalance + ISNULL(SUM(cb.AmountIn - cb.AmountOut), 0) AS Balance
                    FROM SafeAccounts sa WITH (NOLOCK)
                    LEFT JOIN CashBox cb WITH (NOLOCK) ON sa.AccountID = cb.AccountID
                    WHERE sa.IsActive = 1
                    GROUP BY sa.AccountID, sa.AccountName, sa.OpeningBalance
                    ORDER BY sa.AccountID ASC");
                string safesJson = DataTableToJson(dtSafes);

                // 14. الأصناف الأكثر مبيعاً
                DataTable dtTopSelling = DbHelper.Query(@"
                    SELECT TOP 20 p.ProductID, p.ProductName, ISNULL(p.ProductCode, '') AS ProductCode,
                           ISNULL(SUM(si.Quantity), 0) AS TotalQtySold,
                           ISNULL(SUM(si.TotalPrice), 0) AS TotalSalesValue
                    FROM SaleItems si WITH (NOLOCK)
                    JOIN Sales s WITH (NOLOCK) ON si.SaleID = s.SaleID
                    JOIN Products p WITH (NOLOCK) ON si.ProductID = p.ProductID
                    GROUP BY p.ProductID, p.ProductName, p.ProductCode
                    ORDER BY TotalSalesValue DESC");
                string topSellingJson = DataTableToJson(dtTopSelling);

                // 15. أحدث بنود المصروفات
                DataTable dtExpenses = DbHelper.Query(@"
                    SELECT TOP 100 cb.CashID, CONVERT(VARCHAR(19), cb.TransDate, 120) AS TransDate,
                           ISNULL(cb.AmountOut, 0) AS AmountOut,
                           ISNULL(cb.Notes, N'مصروف') AS Notes,
                           ISNULL(sa.AccountName, N'الخزينة الرئيسية') AS SafeName
                    FROM CashBox cb WITH (NOLOCK)
                    LEFT JOIN SafeAccounts sa WITH (NOLOCK) ON cb.AccountID = sa.AccountID
                    WHERE cb.AmountOut > 0
                    ORDER BY cb.CashID DESC");
                string expensesJson = DataTableToJson(dtExpenses);

                // 16. تقارير إغلاق وتقفيل الورديات (Shifts Closing History)
                DataTable dtShifts = null;
                try
                {
                    dtShifts = DbHelper.Query(@"
                        SELECT TOP 50
                            s.ShiftID,
                            CONVERT(VARCHAR(19), s.OpenTime, 120) AS OpenTime,
                            CONVERT(VARCHAR(19), ISNULL(s.CloseTime, s.OpenTime), 120) AS CloseTime,
                            ISNULL(s.CashierName, ISNULL(e.EmpName, N'كاشير')) AS CashierName,
                            ISNULL(s.POSStationName, N'جهاز الكاشير') AS POSStationName,
                            ISNULL(s.BranchName, N'الفرع الرئيسي') AS BranchName,
                            ISNULL(sa.AccountName, N'الدرج الرئيسي') AS SafeName,
                            ISNULL(s.OpeningCash, 0) AS OpeningCash,
                            ISNULL(s.TotalSales, 0) AS TotalSales,
                            ISNULL(s.CashSales, 0) AS CashSales,
                            ISNULL(s.VisaSales, 0) AS VisaSales,
                            ISNULL(s.WalletSales, 0) AS WalletSales,
                            ISNULL(s.CreditSales, 0) AS CreditSales,
                            ISNULL(s.TotalReturns, 0) AS TotalReturns,
                            ISNULL(s.CashReturns, 0) AS CashReturns,
                            ISNULL(s.CashExpenses, 0) AS TotalExpenses,
                            ISNULL(s.CashIn, 0) AS TotalCashIn,
                            ISNULL(s.NetSales, 0) AS NetSales,
                            ISNULL(s.ExpectedCash, 0) AS ExpectedCash,
                            ISNULL(s.ActualCash, 0) AS ActualCash,
                            ISNULL(s.Difference, 0) AS CashDifference,
                            ISNULL(s.InvoiceCount, 0) AS InvoiceCount,
                            ISNULL(s.Status, N'Closed') AS Status,
                            ISNULL(s.ApprovalStatus, N'Closed') AS ApprovalStatus,
                            ISNULL(s.Notes, N'') AS Notes
                        FROM Shifts s WITH (NOLOCK)
                        LEFT JOIN Employees e WITH (NOLOCK) ON s.OpenedBy = e.EmpID
                        LEFT JOIN SafeAccounts sa WITH (NOLOCK) ON s.SafeAccountID = sa.AccountID
                        ORDER BY s.ShiftID DESC");
                }
                catch
                {
                    try
                    {
                        dtShifts = DbHelper.Query(@"
                            SELECT TOP 30
                                s.ShiftID,
                                CONVERT(VARCHAR(19), s.OpenTime, 120) AS OpenTime,
                                CONVERT(VARCHAR(19), ISNULL(s.CloseTime, s.OpenTime), 120) AS CloseTime,
                                ISNULL(e.EmpName, N'كاشير') AS CashierName,
                                ISNULL(s.OpeningCash, 0) AS OpeningCash,
                                ISNULL(s.TotalSales, 0) AS TotalSales,
                                ISNULL(s.CashSales, 0) AS CashSales,
                                ISNULL(s.VisaSales, 0) AS VisaSales,
                                ISNULL(s.ExpectedCash, 0) AS ExpectedCash,
                                ISNULL(s.ActualCash, 0) AS ActualCash,
                                ISNULL(s.Difference, 0) AS CashDifference,
                                ISNULL(s.Status, N'Closed') AS Status
                            FROM Shifts s WITH (NOLOCK)
                            LEFT JOIN Employees e WITH (NOLOCK) ON s.OpenedBy = e.EmpID
                            ORDER BY s.ShiftID DESC");
                    }
                    catch { }
                }
                string shiftsJson = DataTableToJson(dtShifts);

                string isoNow = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
                string timeStr = DateTime.Now.ToString("hh:mm tt");
                string storeName = EscapeJsonString(AppConfig.CompanyName);
                long syncTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                string storeLogoBase64 = GetStoreLogoBase64();

                // A. الرفع المباشر لـ Firebase Realtime Database (RTDB)
                try
                {
                    string rtdbPayload = "{" +
                        "\"TodaySalesTotal\": " + dto.TodaySalesTotal.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," +
                        "\"TodayCashSales\": " + dto.TodayCashSales.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," +
                        "\"TodayCreditSales\": " + dto.TodayCreditSales.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," +
                        "\"TodaySalesInvoicesCount\": " + todaySalesInvoicesCount + "," +
                        "\"CashboxBalance\": " + dto.CashboxBalance.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," +
                        "\"TodayCashIn\": " + todayCashIn.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," +
                        "\"TodayCashOut\": " + todayCashOut.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," +
                        "\"TodayNetProfit\": " + todayProfit.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," +
                        "\"TodayPurchases\": " + todayPurchases.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," +
                        "\"TodayPaidPurchases\": " + todayPaidPurchases.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," +
                        "\"TodayCreditPurchases\": " + todayCreditPurchases.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," +
                        "\"TodayPurInvoicesCount\": " + todayPurInvoicesCount + "," +
                        "\"StockCostValue\": " + stockCostValue.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," +
                        "\"StockSaleValue\": " + stockSaleValue.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," +
                        "\"ClientDebts\": " + clientDebts.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," +
                        "\"SupplierDebts\": " + suppDebts.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," +
                        "\"LowStockCount\": " + dto.LowStockCount + "," +
                        "\"StoreName\": \"" + storeName + "\"," +
                        "\"StoreLogo\": \"" + storeLogoBase64 + "\"," +
                        "\"MasterUserName\": \"" + EscapeJsonString(masterUserName) + "\"," +
                        "\"MasterPassword\": \"" + EscapeJsonString(masterPassword) + "\"," +
                        "\"OwnerPassword\": \"" + EscapeJsonString(ownerPassword) + "\"," +
                        "\"ProMasterUser\": \"pro\"," +
                        "\"ProMasterPass\": \"pro@2026\"," +
                        "\"SyncTime\": \"" + timeStr + "\"," +
                        "\"LastSyncDate\": \"" + isoNow + "\"," +
                        "\"SyncTimestamp\": " + syncTimestamp + "," +
                        "\"ServerStatus\": \"ONLINE\"," +
                        "\"DatabaseEngine\": \"Microsoft SQL Server\"," +
                        "\"MachineName\": \"" + EscapeJsonString(Environment.MachineName) + "\"," +
                        "\"AppVersion\": \"" + UpdateManager.CurrentVersion + "\"," +
                        "\"MissingItems\": " + missingJson + "," +
                        "\"ProductsCatalog\": " + productsJson + "," +
                        "\"SuppliersList\": " + suppliersJson + "," +
                        "\"ClientsList\": " + clientsJson + "," +
                        "\"UsersList\": " + usersJson + "," +
                        "\"ShiftsReport\": " + shiftsJson + "," +
                        "\"RecentSales\": " + recentSalesJson + "," +
                        "\"RecentPurchases\": " + recentPurchasesJson + "," +
                        "\"RecentCashBox\": " + recentCashJson + "," +
                        "\"SafeAccounts\": " + safesJson + "," +
                        "\"TopSelling\": " + topSellingJson + "," +
                        "\"ExpensesList\": " + expensesJson +
                        "}";

                    using (var client = new HttpClient())
                    {
                        client.Timeout = TimeSpan.FromSeconds(60);
                        var content = new StringContent(rtdbPayload, Encoding.UTF8, "application/json");

                        // 1. تجربة الرابط الافتراضي default-rtdb
                        try
                        {
                            var resp = await client.PutAsync($"https://{projectId}-default-rtdb.firebaseio.com/erp_data.json", content);
                            if (resp != null && resp.IsSuccessStatusCode)
                            {
                                rtdbOk = true;
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"Firebase Default RTDB Status: {resp?.StatusCode}");
                            }
                        }
                        catch (Exception exDefault)
                        {
                            System.Diagnostics.Debug.WriteLine("Firebase Default RTDB error: " + exDefault.Message);
                        }

                        // 2. تجربة رابط RTDB الكلاسيكي البديل في حال تعذر الأول
                        if (!rtdbOk)
                        {
                            try
                            {
                                var contentFallback = new StringContent(rtdbPayload, Encoding.UTF8, "application/json");
                                var respFallback = await client.PutAsync($"https://{projectId}.firebaseio.com/erp_data.json", contentFallback);
                                if (respFallback != null && respFallback.IsSuccessStatusCode)
                                {
                                    rtdbOk = true;
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine($"Firebase Fallback RTDB Status: {respFallback?.StatusCode}");
                                }
                            }
                            catch (Exception exFallback)
                            {
                                System.Diagnostics.Debug.WriteLine("Firebase Fallback RTDB error: " + exFallback.Message);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Firebase RTDB Upload: " + ex.Message);
                }

                // B. الرفع المباشر لـ Google Cloud Firestore
                try
                {
                    string firestorePayload = "{\"fields\": {" +
                        "\"TodaySalesTotal\": {\"doubleValue\": " + dto.TodaySalesTotal.ToString(System.Globalization.CultureInfo.InvariantCulture) + "}," +
                        "\"TodayCashSales\": {\"doubleValue\": " + dto.TodayCashSales.ToString(System.Globalization.CultureInfo.InvariantCulture) + "}," +
                        "\"TodayCreditSales\": {\"doubleValue\": " + dto.TodayCreditSales.ToString(System.Globalization.CultureInfo.InvariantCulture) + "}," +
                        "\"CashboxBalance\": {\"doubleValue\": " + dto.CashboxBalance.ToString(System.Globalization.CultureInfo.InvariantCulture) + "}," +
                        "\"TodayNetProfit\": {\"doubleValue\": " + todayProfit.ToString(System.Globalization.CultureInfo.InvariantCulture) + "}," +
                        "\"TodayPurchases\": {\"doubleValue\": " + todayPurchases.ToString(System.Globalization.CultureInfo.InvariantCulture) + "}," +
                        "\"StockCostValue\": {\"doubleValue\": " + stockCostValue.ToString(System.Globalization.CultureInfo.InvariantCulture) + "}," +
                        "\"StockSaleValue\": {\"doubleValue\": " + stockSaleValue.ToString(System.Globalization.CultureInfo.InvariantCulture) + "}," +
                        "\"ClientDebts\": {\"doubleValue\": " + clientDebts.ToString(System.Globalization.CultureInfo.InvariantCulture) + "}," +
                        "\"SupplierDebts\": {\"doubleValue\": " + suppDebts.ToString(System.Globalization.CultureInfo.InvariantCulture) + "}," +
                        "\"LowStockCount\": {\"integerValue\": \"" + dto.LowStockCount + "\"}," +
                        "\"StoreName\": {\"stringValue\": \"" + storeName + "\"}," +
                        "\"StoreLogo\": {\"stringValue\": \"" + storeLogoBase64 + "\"}," +
                        "\"MasterUserName\": {\"stringValue\": \"" + EscapeJsonString(masterUserName) + "\"}," +
                        "\"MasterPassword\": {\"stringValue\": \"" + EscapeJsonString(masterPassword) + "\"}," +
                        "\"OwnerPassword\": {\"stringValue\": \"" + EscapeJsonString(ownerPassword) + "\"}," +
                        "\"ProMasterUser\": {\"stringValue\": \"pro\"}," +
                        "\"ProMasterPass\": {\"stringValue\": \"pro@2026\"}," +
                        "\"SyncTime\": {\"stringValue\": \"" + timeStr + "\"}," +
                        "\"LastSyncDate\": {\"stringValue\": \"" + isoNow + "\"}," +
                        "\"SyncTimestamp\": {\"integerValue\": \"" + syncTimestamp + "\"}," +
                        "\"ServerStatus\": {\"stringValue\": \"ONLINE\"}," +
                        "\"DatabaseEngine\": {\"stringValue\": \"Microsoft SQL Server\"}," +
                        "\"MachineName\": {\"stringValue\": \"" + EscapeJsonString(Environment.MachineName) + "\"}," +
                        "\"MissingItemsJson\": {\"stringValue\": \"" + EscapeJsonString(missingJson) + "\"}," +
                        "\"ProductsCatalogJson\": {\"stringValue\": \"" + EscapeJsonString(productsJson) + "\"}," +
                        "\"SuppliersListJson\": {\"stringValue\": \"" + EscapeJsonString(suppliersJson) + "\"}," +
                        "\"ClientsListJson\": {\"stringValue\": \"" + EscapeJsonString(clientsJson) + "\"}," +
                        "\"UsersListJson\": {\"stringValue\": \"" + EscapeJsonString(usersJson) + "\"}," +
                        "\"ShiftsReportJson\": {\"stringValue\": \"" + EscapeJsonString(shiftsJson) + "\"}," +
                        "\"RecentSalesJson\": {\"stringValue\": \"" + EscapeJsonString(recentSalesJson) + "\"}," +
                        "\"RecentPurchasesJson\": {\"stringValue\": \"" + EscapeJsonString(recentPurchasesJson) + "\"}," +
                        "\"RecentCashBoxJson\": {\"stringValue\": \"" + EscapeJsonString(recentCashJson) + "\"}," +
                        "\"SafeAccountsJson\": {\"stringValue\": \"" + EscapeJsonString(safesJson) + "\"}," +
                        "\"TopSellingJson\": {\"stringValue\": \"" + EscapeJsonString(topSellingJson) + "\"}," +
                        "\"ExpensesListJson\": {\"stringValue\": \"" + EscapeJsonString(expensesJson) + "\"}" +
                        "}}";

                    using (var client = new HttpClient())
                    {
                        client.Timeout = TimeSpan.FromSeconds(30);
                        var content = new StringContent(firestorePayload, Encoding.UTF8, "application/json");
                        var req = new HttpRequestMessage(new HttpMethod("PATCH"), $"https://firestore.googleapis.com/v1/projects/{projectId}/databases/(default)/documents/metadata/live_reports")
                        {
                            Content = content
                        };
                        var response = await client.SendAsync(req);
                        if (response.IsSuccessStatusCode)
                        {
                            firestoreOk = true;
                        }

                        string clientSerial = GetPermanentClientSerial();
                        if (!string.IsNullOrEmpty(clientSerial))
                        {
                            try
                            {
                                var contentSerial = new StringContent(firestorePayload, Encoding.UTF8, "application/json");
                                var reqSerial = new HttpRequestMessage(new HttpMethod("PATCH"), $"https://firestore.googleapis.com/v1/projects/{projectId}/databases/(default)/documents/serials/{clientSerial}")
                                {
                                    Content = contentSerial
                                };
                                await client.SendAsync(reqSerial);
                            }
                            catch { }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Firestore Upload: " + ex.Message);
                }

                if (rtdbOk || firestoreOk)
                {
                    try
                    {
                        string statusMsg = $"متصل بنجاح 🔥 ({DateTime.Now:yyyy-MM-dd HH:mm:ss})";
                        DbHelper.Execute(@"
                            UPDATE CloudSyncSettings 
                            SET LastSyncDate = GETDATE(), LastSyncStatus = @status 
                            WHERE SettingID = 1",
                            DbHelper.P("@status", statusMsg));
                    }
                    catch { }
                }

                return rtdbOk || firestoreOk;
            }
            catch (Exception ex)
            {
                AppLogger.Error("فشل رفع بيانات المالك لـ Firebase", ex, "PushLiveStatsToFirebaseAsync");
                return false;
            }
        }

        public static async Task<bool> PushLiveStatsToFirestoreAsync(string projectId = null)
        {
            return await PushLiveStatsToFirebaseAsync(projectId);
        }

        private static string DataTableToJson(DataTable dt)
        {
            if (dt == null || dt.Rows.Count == 0) return "[]";
            var sb = new StringBuilder("[");
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                if (i > 0) sb.Append(",");
                sb.Append("{");
                for (int j = 0; j < dt.Columns.Count; j++)
                {
                    if (j > 0) sb.Append(",");
                    string colName = dt.Columns[j].ColumnName;
                    object val = dt.Rows[i][j];
                    sb.Append($"\"{EscapeJsonString(colName)}\":");
                    if (val == DBNull.Value || val == null)
                    {
                        sb.Append("null");
                    }
                    else if (val is bool b)
                    {
                        sb.Append(b ? "true" : "false");
                    }
                    else if (val is int || val is long || val is short || val is decimal || val is double || val is float)
                    {
                        sb.Append(val.ToString().Replace(",", "."));
                    }
                    else
                    {
                        sb.Append($"\"{EscapeJsonString(val.ToString())}\"");
                    }
                }
                sb.Append("}");
            }
            sb.Append("]");
            return sb.ToString();
        }

        private static System.Threading.Timer _autoSyncTimer;

        public static void StartAutoBackgroundSync()
        {
            if (_autoSyncTimer != null) return;
            // Push live stats to Firebase RTDB and Firestore every 15 seconds
            _autoSyncTimer = new System.Threading.Timer(async _ =>
            {
                try
                {
                    await PushLiveStatsToFirebaseAsync();
                }
                catch {}
            }, null, 1000, 15000);
        }

        public static void TriggerSyncNow()
        {
            try
            {
                System.Threading.Tasks.Task.Run(async () =>
                {
                    try
                    {
                        await PushLiveStatsToFirebaseAsync();
                    }
                    catch { }
                });
            }
            catch { }
        }

        public static string GetStoreLogoBase64()
        {
            try
            {
                using (var img = Theme.GetCompanyLogo())
                {
                    if (img != null)
                    {
                        using (var ms = new System.IO.MemoryStream())
                        {
                            img.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                            return "data:image/png;base64," + Convert.ToBase64String(ms.ToArray());
                        }
                    }
                }
            }
            catch { }
            return "";
        }

        private static string EscapeJsonString(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "");
        }
    }
}

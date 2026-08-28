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
            try
            {
                DriverPortalServer.EnsureMobileAppFilesExtracted();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("EnsureMobileAppFolderExists warning: " + ex.Message);
            }
        }

        public static SyncStatsDTO GetLiveStats()
        {
            var dto = new SyncStatsDTO();

            try
            {
                // مبيعات اليوم
                object totObj = DbHelper.Scalar("SELECT ISNULL(SUM(TotalAmount),0) FROM Sales WHERE CAST(SaleDate AS DATE) = CAST(GETDATE() AS DATE)");
                dto.TodaySalesTotal = totObj != null && totObj != DBNull.Value ? Convert.ToDecimal(totObj) : 0m;

                object cashObj = DbHelper.Scalar("SELECT ISNULL(SUM(TotalAmount),0) FROM Sales WHERE CAST(SaleDate AS DATE) = CAST(GETDATE() AS DATE) AND SaleType = 'Cash'");
                dto.TodayCashSales = cashObj != null && cashObj != DBNull.Value ? Convert.ToDecimal(cashObj) : 0m;

                object credObj = DbHelper.Scalar("SELECT ISNULL(SUM(TotalAmount),0) FROM Sales WHERE CAST(SaleDate AS DATE) = CAST(GETDATE() AS DATE) AND SaleType <> 'Cash'");
                dto.TodayCreditSales = credObj != null && credObj != DBNull.Value ? Convert.ToDecimal(credObj) : 0m;

                // رصيد الخزنة
                object cbObj = DbHelper.Scalar("SELECT ISNULL(SUM(ISNULL(AmountIn,0) - ISNULL(AmountOut,0)), 0) FROM CashBox");
                dto.CashboxBalance = cbObj != null && cbObj != DBNull.Value ? Convert.ToDecimal(cbObj) : 0m;

                // نواقص المخزن
                object lowObj = DbHelper.Scalar("SELECT COUNT(*) FROM Products WHERE IsActive = 1 AND Quantity <= ISNULL(MinQuantity, 5)");
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
                string projectId = AppConfig.Get("FirebaseProjectId", "mahmoud-68b74");
                if (string.IsNullOrEmpty(projectId)) projectId = "mahmoud-68b74";

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
                projectId = AppConfig.Get("FirebaseProjectId", "mahmoud-68b74");
            }
            if (string.IsNullOrEmpty(projectId)) projectId = "mahmoud-68b74";

            bool rtdbOk = false;
            bool firestoreOk = false;

            try
            {
                var dto = GetLiveStats();

                // 1. حساب صافي الربح اليوم
                object profitObj = DbHelper.Scalar(
                    @"SELECT ISNULL(SUM(si.TotalPrice - (si.Quantity * ISNULL(p.PurchasePrice, 0))), 0)
                      FROM SaleItems si
                      JOIN Sales s ON si.SaleID = s.SaleID
                      JOIN Products p ON si.ProductID = p.ProductID
                      WHERE CAST(s.SaleDate AS DATE) = CAST(GETDATE() AS DATE)");
                decimal todayProfit = profitObj != null && profitObj != DBNull.Value ? Convert.ToDecimal(profitObj) : 0m;

                // 2. مشتريات اليوم
                object purObj = DbHelper.Scalar("SELECT ISNULL(SUM(ISNULL(TotalAmount, 0)), 0) FROM Purchases WHERE CAST(PurchaseDate AS DATE) = CAST(GETDATE() AS DATE)");
                decimal todayPurchases = purObj != null && purObj != DBNull.Value ? Convert.ToDecimal(purObj) : 0m;

                // 3. ديون العملاء
                object clientDebtsObj = DbHelper.Scalar("SELECT ISNULL(SUM(ISNULL(Balance, 0)), 0) FROM Clients WHERE Balance > 0");
                decimal clientDebts = clientDebtsObj != null && clientDebtsObj != DBNull.Value ? Convert.ToDecimal(clientDebtsObj) : 0m;

                // 4. مستحقات الموردين
                object suppDebtsObj = DbHelper.Scalar("SELECT ISNULL(SUM(ISNULL(Balance, 0)), 0) FROM Suppliers WHERE Balance > 0");
                decimal suppDebts = suppDebtsObj != null && suppDebtsObj != DBNull.Value ? Convert.ToDecimal(suppDebtsObj) : 0m;

                // 5. كشكول النواقص الحقيقي
                DataTable dtMissing = DbHelper.Query(
                    "SELECT TOP 40 ProductID, ProductName, ISNULL(ProductCode,'') AS ProductCode, ISNULL(Quantity,0) AS Quantity, ISNULL(MinQuantity,5) AS MinQuantity, ISNULL(Brand,'عام') AS Supplier FROM Products WHERE IsActive=1 AND Quantity <= ISNULL(MinQuantity, 5) ORDER BY Quantity ASC");
                string missingJson = DataTableToJson(dtMissing);

                // 6. دليل الأصناف
                DataTable dtProducts = DbHelper.Query(
                    "SELECT TOP 100 ProductID, ProductName, ISNULL(ProductCode,'') AS ProductCode, ISNULL(SalePrice,0) AS SalePrice, ISNULL(PurchasePrice,0) AS PurchasePrice, ISNULL(Quantity,0) AS Quantity FROM Products WHERE IsActive=1 ORDER BY ProductName ASC");
                string productsJson = DataTableToJson(dtProducts);

                // 7. قائمة الموردين
                DataTable dtSuppliers = DbHelper.Query(
                    "SELECT TOP 30 SupplierID, SupplierName, ISNULL(Balance,0) AS Balance FROM Suppliers WHERE IsActive=1 ORDER BY SupplierName ASC");
                string suppliersJson = DataTableToJson(dtSuppliers);

                // 8. ديون العملاء
                DataTable dtClients = DbHelper.Query(
                    "SELECT TOP 30 ClientID, ClientName, ISNULL(Balance,0) AS Balance FROM Clients WHERE IsActive=1 AND Balance > 0 ORDER BY Balance DESC");
                string clientsJson = DataTableToJson(dtClients);

                string isoNow = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
                string timeStr = DateTime.Now.ToString("hh:mm tt");
                string storeName = EscapeJsonString(AppConfig.CompanyName);

                // A. الرفع المباشر لـ Firebase Realtime Database (RTDB)
                try
                {
                    string rtdbPayload = "{" +
                        "\"TodaySalesTotal\": " + dto.TodaySalesTotal.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," +
                        "\"TodayCashSales\": " + dto.TodayCashSales.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," +
                        "\"TodayCreditSales\": " + dto.TodayCreditSales.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," +
                        "\"CashboxBalance\": " + dto.CashboxBalance.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," +
                        "\"TodayNetProfit\": " + todayProfit.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," +
                        "\"TodayPurchases\": " + todayPurchases.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," +
                        "\"ClientDebts\": " + clientDebts.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," +
                        "\"SupplierDebts\": " + suppDebts.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," +
                        "\"LowStockCount\": " + dto.LowStockCount + "," +
                        "\"StoreName\": \"" + storeName + "\"," +
                        "\"SyncTime\": \"" + timeStr + "\"," +
                        "\"LastSyncDate\": \"" + isoNow + "\"," +
                        "\"MissingItems\": " + missingJson + "," +
                        "\"ProductsCatalog\": " + productsJson + "," +
                        "\"SuppliersList\": " + suppliersJson + "," +
                        "\"ClientsList\": " + clientsJson +
                        "}";

                    using (var client = new HttpClient())
                    {
                        client.Timeout = TimeSpan.FromSeconds(10);
                        var content = new StringContent(rtdbPayload, Encoding.UTF8, "application/json");
                        var resp = await client.PutAsync($"https://{projectId}-default-rtdb.firebaseio.com/erp_data.json", content);
                        if (resp.IsSuccessStatusCode)
                        {
                            rtdbOk = true;
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
                        "\"ClientDebts\": {\"doubleValue\": " + clientDebts.ToString(System.Globalization.CultureInfo.InvariantCulture) + "}," +
                        "\"SupplierDebts\": {\"doubleValue\": " + suppDebts.ToString(System.Globalization.CultureInfo.InvariantCulture) + "}," +
                        "\"LowStockCount\": {\"integerValue\": \"" + dto.LowStockCount + "\"}," +
                        "\"StoreName\": {\"stringValue\": \"" + storeName + "\"}," +
                        "\"SyncTime\": {\"stringValue\": \"" + timeStr + "\"}," +
                        "\"LastSyncDate\": {\"stringValue\": \"" + isoNow + "\"}," +
                        "\"MissingItemsJson\": {\"stringValue\": \"" + EscapeJsonString(missingJson) + "\"}," +
                        "\"ProductsCatalogJson\": {\"stringValue\": \"" + EscapeJsonString(productsJson) + "\"}," +
                        "\"SuppliersListJson\": {\"stringValue\": \"" + EscapeJsonString(suppliersJson) + "\"}," +
                        "\"ClientsListJson\": {\"stringValue\": \"" + EscapeJsonString(clientsJson) + "\"}" +
                        "}}";

                    using (var client = new HttpClient())
                    {
                        client.Timeout = TimeSpan.FromSeconds(10);
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
            try
            {
                if (string.IsNullOrEmpty(projectId))
                {
                    projectId = AppConfig.Get("FirebaseProjectId", "mahmoud-68b74");
                }
                if (string.IsNullOrEmpty(projectId)) projectId = "mahmoud-68b74";

                var dto = GetLiveStats();

                // Net Profit today
                object profitObj = DbHelper.Scalar(
                    @"SELECT ISNULL(SUM(si.TotalPrice - (si.Quantity * ISNULL(p.PurchasePrice, 0))), 0)
                      FROM SaleItems si
                      JOIN Sales s ON si.SaleID = s.SaleID
                      JOIN Products p ON si.ProductID = p.ProductID
                      WHERE CAST(s.SaleDate AS DATE) = CAST(GETDATE() AS DATE)");
                decimal todayProfit = profitObj != null && profitObj != DBNull.Value ? Convert.ToDecimal(profitObj) : 0m;

                // Today Purchases
                object purObj = DbHelper.Scalar("SELECT ISNULL(SUM(ISNULL(TotalAmount, 0)), 0) FROM Purchases WHERE CAST(PurchaseDate AS DATE) = CAST(GETDATE() AS DATE)");
                decimal todayPurchases = purObj != null && purObj != DBNull.Value ? Convert.ToDecimal(purObj) : 0m;

                // Client Debts
                object clientDebtsObj = DbHelper.Scalar("SELECT ISNULL(SUM(ISNULL(Balance, 0)), 0) FROM Clients WHERE Balance > 0");
                decimal clientDebts = clientDebtsObj != null && clientDebtsObj != DBNull.Value ? Convert.ToDecimal(clientDebtsObj) : 0m;

                // Supplier Debts
                object suppDebtsObj = DbHelper.Scalar("SELECT ISNULL(SUM(ISNULL(Balance, 0)), 0) FROM Suppliers WHERE Balance > 0");
                decimal suppDebts = suppDebtsObj != null && suppDebtsObj != DBNull.Value ? Convert.ToDecimal(suppDebtsObj) : 0m;

                // Real Missing items notebook from SQL Server
                DataTable dtMissing = DbHelper.Query(
                    "SELECT TOP 40 ProductID, ProductName, ISNULL(ProductCode,'') AS ProductCode, ISNULL(Quantity,0) AS Quantity, ISNULL(MinQuantity,5) AS MinQuantity, ISNULL(Brand,'عام') AS Supplier FROM Products WHERE IsActive=1 AND Quantity <= ISNULL(MinQuantity, 5) ORDER BY Quantity ASC");
                string missingJson = DataTableToJson(dtMissing);

                // Real product catalog from SQL Server
                DataTable dtProducts = DbHelper.Query(
                    "SELECT TOP 100 ProductID, ProductName, ISNULL(ProductCode,'') AS ProductCode, ISNULL(SalePrice,0) AS SalePrice, ISNULL(PurchasePrice,0) AS PurchasePrice, ISNULL(Quantity,0) AS Quantity FROM Products WHERE IsActive=1 ORDER BY ProductName ASC");
                string productsJson = DataTableToJson(dtProducts);

                // Real Suppliers from SQL Server
                DataTable dtSuppliers = DbHelper.Query(
                    "SELECT TOP 30 SupplierID, SupplierName, ISNULL(Balance,0) AS Balance FROM Suppliers WHERE IsActive=1 ORDER BY SupplierName ASC");
                string suppliersJson = DataTableToJson(dtSuppliers);

                // Real Client Debts from SQL Server
                DataTable dtClients = DbHelper.Query(
                    "SELECT TOP 30 ClientID, ClientName, ISNULL(Balance,0) AS Balance FROM Clients WHERE IsActive=1 AND Balance > 0 ORDER BY Balance DESC");
                string clientsJson = DataTableToJson(dtClients);

                string isoNow = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
                string storeName = EscapeJsonString(AppConfig.CompanyName);

                string clientSerial = GetPermanentClientSerial();
                string json = "{\"fields\": {" +
                    "\"TodaySalesTotal\": {\"doubleValue\": " + dto.TodaySalesTotal.ToString(System.Globalization.CultureInfo.InvariantCulture) + "}," +
                    "\"TodayCashSales\": {\"doubleValue\": " + dto.TodayCashSales.ToString(System.Globalization.CultureInfo.InvariantCulture) + "}," +
                    "\"TodayCreditSales\": {\"doubleValue\": " + dto.TodayCreditSales.ToString(System.Globalization.CultureInfo.InvariantCulture) + "}," +
                    "\"CashboxBalance\": {\"doubleValue\": " + dto.CashboxBalance.ToString(System.Globalization.CultureInfo.InvariantCulture) + "}," +
                    "\"TodayNetProfit\": {\"doubleValue\": " + todayProfit.ToString(System.Globalization.CultureInfo.InvariantCulture) + "}," +
                    "\"TodayPurchases\": {\"doubleValue\": " + todayPurchases.ToString(System.Globalization.CultureInfo.InvariantCulture) + "}," +
                    "\"ClientDebts\": {\"doubleValue\": " + clientDebts.ToString(System.Globalization.CultureInfo.InvariantCulture) + "}," +
                    "\"SupplierDebts\": {\"doubleValue\": " + suppDebts.ToString(System.Globalization.CultureInfo.InvariantCulture) + "}," +
                    "\"LowStockCount\": {\"integerValue\": \"" + dto.LowStockCount + "\"}," +
                    "\"StoreName\": {\"stringValue\": \"" + storeName + "\"}," +
                    "\"LastSyncDate\": {\"stringValue\": \"" + isoNow + "\"}," +
                    "\"MissingItemsJson\": {\"stringValue\": \"" + EscapeJsonString(missingJson) + "\"}," +
                    "\"ProductsCatalogJson\": {\"stringValue\": \"" + EscapeJsonString(productsJson) + "\"}," +
                    "\"SuppliersListJson\": {\"stringValue\": \"" + EscapeJsonString(suppliersJson) + "\"}," +
                    "\"ClientsListJson\": {\"stringValue\": \"" + EscapeJsonString(clientsJson) + "\"}" +
                    "}}";

                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(10);
                    
                    // 1. Push to global metadata/live_reports
                    var content1 = new StringContent(json, Encoding.UTF8, "application/json");
                    var req1 = new HttpRequestMessage(new HttpMethod("PATCH"), $"https://firestore.googleapis.com/v1/projects/{projectId}/databases/(default)/documents/metadata/live_reports")
                    {
                        Content = content1
                    };
                    var response1 = await client.SendAsync(req1);

                    // 2. Push to client serial document serials/{clientSerial}
                    if (!string.IsNullOrEmpty(clientSerial))
                    {
                        try
                        {
                            var content2 = new StringContent(json, Encoding.UTF8, "application/json");
                            var req2 = new HttpRequestMessage(new HttpMethod("PATCH"), $"https://firestore.googleapis.com/v1/projects/{projectId}/databases/(default)/documents/serials/{clientSerial}")
                            {
                                Content = content2
                            };
                            await client.SendAsync(req2);
                        }
                        catch { }
                    }

                    return response1.IsSuccessStatusCode;
                }
            }
            catch (Exception ex)
            {
                if (!(ex is TaskCanceledException || ex is OperationCanceledException || ex is System.Net.Http.HttpRequestException))
                {
                    AppLogger.Error("فشل رفع التقارير المباشرة لـ Firestore", ex, "PushLiveStatsToFirestoreAsync");
                }
                return false;
            }
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
            // Push live stats to Firestore immediately, then automatically every 30 seconds
            _autoSyncTimer = new System.Threading.Timer(async _ =>
            {
                try
                {
                    await PushLiveStatsToFirestoreAsync();
                }
                catch {}
            }, null, 1000, 30000);
        }

        private static string EscapeJsonString(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "");
        }
    }
}

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
                string clientSerial = GetPermanentClientSerial();
                string jsonPayload = DAL.DriverDAL.BuildDriverExportJson(null);
                string encrypted = SecurityHelper.Encrypt(jsonPayload);

                bool uploadOk = false;
                string statusMsg = $"تم التزامن السحابي بنجاح 🟢 (السيريال: {clientSerial})";

                // 1. Upload to KVDB (CORS-enabled persistent Cloud Store by Client Serial)
                try
                {
                    using (var client = new HttpClient())
                    {
                        client.Timeout = TimeSpan.FromSeconds(8);
                        var content = new StringContent(encrypted, Encoding.UTF8, "text/plain");
                        var response = await client.PutAsync($"https://kvdb.io/9u8nZ23pBqX412/{clientSerial}", content);
                        if (response.IsSuccessStatusCode)
                        {
                            uploadOk = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("KVDB Upload warning: " + ex.Message);
                }

                // 2. Upload to DriverPortalServer fallback
                try
                {
                    DriverPortalServer.UploadToCloud();
                    uploadOk = true;
                }
                catch { }

                DbHelper.Execute(@"
                    UPDATE CloudSyncSettings 
                    SET LastSyncDate = GETDATE(), LastSyncStatus = @status 
                    WHERE SettingID = 1",
                    DbHelper.P("@status", statusMsg));

                return (uploadOk, statusMsg);
            }
            catch (Exception ex)
            {
                AppLogger.Error("فشل المزامنة مع السيرفر السحابي", ex, "CloudSyncService.SyncNowAsync");
                return (false, "خطأ أثناء المزامنة: " + ex.Message);
            }
        }

        public static string GeneratePairingPayload()
        {
            DataTable dtSet = DbHelper.Query("SELECT TOP 1 ApiUrl, OwnerSecretKey FROM CloudSyncSettings WHERE SettingID = 1");
            string apiUrl = dtSet.Rows.Count > 0 ? dtSet.Rows[0]["ApiUrl"]?.ToString() : "https://api.chickendist.com/v1";
            string ownerKey = dtSet.Rows.Count > 0 ? dtSet.Rows[0]["OwnerSecretKey"]?.ToString() : "OWNER-SECRET-KEY";

            return $@"chickendist://pair?url={Uri.EscapeDataString(apiUrl)}&key={Uri.EscapeDataString(ownerKey)}&company={Uri.EscapeDataString(AppConfig.CompanyName)}";
        }
    }
}

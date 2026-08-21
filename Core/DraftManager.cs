using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Web.Script.Serialization;

namespace ChickenDist.Core
{
    public class SaleDraftData
    {
        public int ClientID { get; set; }
        public string ClientName { get; set; }
        public string InvoiceType { get; set; }
        public int WarehouseID { get; set; }
        public int? DriverID { get; set; }
        public decimal DiscountVal { get; set; }
        public string DiscountType { get; set; }
        public decimal VisaPaid { get; set; }
        public int? VisaAccountID { get; set; }
        public decimal PaidAmount { get; set; }
        public string Notes { get; set; }
        public List<SaleDraftItem> Items { get; set; } = new List<SaleDraftItem>();
    }

    public class SaleDraftItem
    {
        public int ProductID { get; set; }
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public string Unit { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineDiscount { get; set; }
        public decimal Factor { get; set; } = 1m;
        public decimal LineTotal { get; set; }
        public int? BatchID { get; set; }
        public string ExpiryDate { get; set; }
        public string IMEI { get; set; }
    }

    public class PurchaseDraftData
    {
        public int SupplierID { get; set; }
        public string SupplierName { get; set; }
        public string SupplierInvoiceNo { get; set; }
        public int WarehouseID { get; set; }
        public string PaymentType { get; set; }
        public decimal DiscountVal { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal PaidAmount { get; set; }
        public string Notes { get; set; }
        public List<PurchaseDraftItem> Items { get; set; } = new List<PurchaseDraftItem>();
    }

    public class PurchaseDraftItem
    {
        public int ProductID { get; set; }
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public string Unit { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineDiscount { get; set; }
        public decimal Factor { get; set; } = 1m;
        public decimal LineTotal { get; set; }
        public string ExpiryDate { get; set; }
        public string IMEI { get; set; }
    }

    public class InventoryDraftData
    {
        public int WarehouseID { get; set; }
        public string WarehouseName { get; set; }
        public string Notes { get; set; }
        public Dictionary<string, decimal> EnteredActualQty { get; set; } = new Dictionary<string, decimal>();
        public List<InventoryDraftItemDetail> ItemsDetails { get; set; } = new List<InventoryDraftItemDetail>();
    }

    public class InventoryDraftItemDetail
    {
        public int ProductID { get; set; }
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public string Unit { get; set; }
        public decimal BookQty { get; set; }
        public decimal ActualQty { get; set; }
        public decimal DiffQty { get; set; }
        public decimal SalePrice { get; set; }
        public string ShelfLocation { get; set; }
    }

    public static class DraftManager
    {
        private static readonly JavaScriptSerializer _json = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
        private static readonly string _localDraftFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Drafts");

        static DraftManager()
        {
            try
            {
                if (!Directory.Exists(_localDraftFolder))
                    Directory.CreateDirectory(_localDraftFolder);
            }
            catch { }
        }

        public static void EnsureDraftsTable()
        {
            try
            {
                DbHelper.Execute(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'IncompleteDrafts')
                BEGIN
                    CREATE TABLE IncompleteDrafts (
                        DraftID     INT IDENTITY(1,1) PRIMARY KEY,
                        DraftType   NVARCHAR(30) NOT NULL,
                        DraftKey    NVARCHAR(100) NOT NULL,
                        UserID      INT NULL,
                        TargetID    INT NULL,
                        TargetName  NVARCHAR(200) NULL,
                        InvoiceType NVARCHAR(50) NULL,
                        TotalAmount DECIMAL(18,2) DEFAULT 0,
                        ItemCount   INT DEFAULT 0,
                        DraftData   NVARCHAR(MAX) NOT NULL,
                        CreatedAt   DATETIME DEFAULT GETDATE(),
                        UpdatedAt   DATETIME DEFAULT GETDATE(),
                        IsRecovered BIT DEFAULT 0
                    );
                    CREATE INDEX IX_IncompleteDrafts_Type ON IncompleteDrafts(DraftType, IsRecovered, UpdatedAt);
                END");
            }
            catch (Exception ex)
            {
                AppLogger.Error("DraftManager.EnsureDraftsTable", ex);
            }
        }

        public static void SaveDraft(string draftType, string draftKey, int? userId, int? targetId, string targetName, string invoiceType, decimal totalAmount, int itemCount, object dataObj)
        {
            if (itemCount <= 0 && totalAmount <= 0)
            {
                DeleteDraft(draftKey);
                return;
            }

            try
            {
                string jsonStr = _json.Serialize(dataObj);

                // 1. Local backup on disk (survives offline or DB disconnect during power cut)
                try
                {
                    string safeFileName = "draft_" + draftKey.Replace(" ", "_").Replace(":", "_").Replace("\\", "_").Replace("/", "_") + ".json";
                    string localPath = Path.Combine(_localDraftFolder, safeFileName);
                    File.WriteAllText(localPath, jsonStr, System.Text.Encoding.UTF8);
                }
                catch { }

                // 2. Central database storage
                string sql = @"
                IF EXISTS (SELECT 1 FROM IncompleteDrafts WHERE DraftKey = @key AND IsRecovered = 0)
                BEGIN
                    UPDATE IncompleteDrafts
                    SET DraftType = @type,
                        UserID = @uid,
                        TargetID = @tid,
                        TargetName = @tname,
                        InvoiceType = @invType,
                        TotalAmount = @total,
                        ItemCount = @cnt,
                        DraftData = @data,
                        UpdatedAt = GETDATE()
                    WHERE DraftKey = @key AND IsRecovered = 0;
                END
                ELSE
                BEGIN
                    INSERT INTO IncompleteDrafts (DraftType, DraftKey, UserID, TargetID, TargetName, InvoiceType, TotalAmount, ItemCount, DraftData, CreatedAt, UpdatedAt, IsRecovered)
                    VALUES (@type, @key, @uid, @tid, @tname, @invType, @total, @cnt, @data, GETDATE(), GETDATE(), 0);
                END";

                DbHelper.Execute(sql,
                    DbHelper.P("@type", draftType),
                    DbHelper.P("@key", draftKey),
                    DbHelper.P("@uid", userId.HasValue ? (object)userId.Value : DBNull.Value),
                    DbHelper.P("@tid", targetId.HasValue ? (object)targetId.Value : DBNull.Value),
                    DbHelper.P("@tname", targetName ?? (object)DBNull.Value),
                    DbHelper.P("@invType", invoiceType ?? (object)DBNull.Value),
                    DbHelper.P("@total", totalAmount),
                    DbHelper.P("@cnt", itemCount),
                    DbHelper.P("@data", jsonStr));
            }
            catch (Exception ex)
            {
                AppLogger.Error("DraftManager.SaveDraft", ex);
            }
        }

        public static void DeleteDraft(string draftKey)
        {
            if (string.IsNullOrEmpty(draftKey)) return;
            try
            {
                // Remove local file
                try
                {
                    string safeFileName = "draft_" + draftKey.Replace(" ", "_").Replace(":", "_").Replace("\\", "_").Replace("/", "_") + ".json";
                    string localPath = Path.Combine(_localDraftFolder, safeFileName);
                    if (File.Exists(localPath)) File.Delete(localPath);
                }
                catch { }

                // Remove / Mark in DB
                DbHelper.Execute("DELETE FROM IncompleteDrafts WHERE DraftKey = @key", DbHelper.P("@key", draftKey));
            }
            catch (Exception ex)
            {
                AppLogger.Error("DraftManager.DeleteDraft", ex);
            }
        }

        public static void DeleteDraftByID(int draftId)
        {
            if (draftId <= 0) return;
            try
            {
                var rowObj = DbHelper.Scalar("SELECT DraftKey FROM IncompleteDrafts WHERE DraftID = @id", DbHelper.P("@id", draftId));
                if (rowObj != null && rowObj != DBNull.Value)
                {
                    string k = rowObj.ToString();
                    try
                    {
                        string safeFileName = "draft_" + k.Replace(" ", "_").Replace(":", "_").Replace("\\", "_").Replace("/", "_") + ".json";
                        string localPath = Path.Combine(_localDraftFolder, safeFileName);
                        if (File.Exists(localPath)) File.Delete(localPath);
                    }
                    catch { }
                }

                DbHelper.Execute("DELETE FROM IncompleteDrafts WHERE DraftID = @id", DbHelper.P("@id", draftId));
            }
            catch (Exception ex)
            {
                AppLogger.Error("DraftManager.DeleteDraftByID", ex);
            }
        }

        public static void MarkRecovered(int draftId)
        {
            if (draftId <= 0) return;
            try
            {
                DbHelper.Execute("UPDATE IncompleteDrafts SET IsRecovered = 1 WHERE DraftID = @id", DbHelper.P("@id", draftId));
            }
            catch { }
        }

        public static DataTable GetIncompleteDrafts(string draftType, DateTime from, DateTime to, string searchTerm = "")
        {
            EnsureDraftsTable();
            var prms = new List<SqlParameter>
            {
                DbHelper.P("@f", from.Date),
                DbHelper.P("@t", to.Date.AddDays(1).AddSeconds(-1))
            };

            string filter = " WHERE IsRecovered = 0 AND UpdatedAt BETWEEN @f AND @t ";

            if (!string.IsNullOrEmpty(draftType) && draftType != "ALL")
            {
                filter += " AND DraftType = @dtype ";
                prms.Add(DbHelper.P("@dtype", draftType));
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                filter += " AND (TargetName LIKE @term OR InvoiceType LIKE @term OR DraftKey LIKE @term) ";
                prms.Add(DbHelper.P("@term", "%" + searchTerm + "%"));
            }

            string sql = $@"
                SELECT 
                    d.DraftID,
                    d.DraftType,
                    d.DraftKey,
                    d.UpdatedAt,
                    d.TargetID,
                    d.TargetName,
                    d.InvoiceType,
                    d.TotalAmount,
                    d.ItemCount,
                    COALESCE(e.EmpName, N'المدير العام') AS CreatedBy,
                    d.DraftData
                FROM IncompleteDrafts d
                LEFT JOIN Employees e ON d.UserID = e.EmpID
                {filter}
                ORDER BY d.UpdatedAt DESC";

            try
            {
                return DbHelper.Query(sql, prms.ToArray());
            }
            catch
            {
                return new DataTable();
            }
        }

        public static T Deserialize<T>(string jsonStr) where T : class, new()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(jsonStr)) return new T();
                return _json.Deserialize<T>(jsonStr) ?? new T();
            }
            catch
            {
                return new T();
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using ChickenDist.Core;

namespace ChickenDist.DAL
{
    public class OnlineOrderItemDTO
    {
        public int ItemRowID { get; set; }
        public int OnlineOrderID { get; set; }
        public int? ProductID { get; set; }
        public string ProductName { get; set; }
        public string UnitName { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public string Notes { get; set; }
    }

    public class OnlineOrderDTO
    {
        public int OnlineOrderID { get; set; }
        public string RemoteOrderID { get; set; }
        public string OrderNumber { get; set; }
        public DateTime OrderDate { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public string CustomerAddress { get; set; }
        public string Notes { get; set; }
        public decimal SubTotal { get; set; }
        public decimal DeliveryCharge { get; set; }
        public decimal TotalAmount { get; set; }
        public string PriceTier { get; set; }
        public string Status { get; set; }
        public int? CreatedSaleID { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<OnlineOrderItemDTO> Items { get; set; } = new List<OnlineOrderItemDTO>();
    }

    public static class OnlineOrdersDAL
    {
        public static DataTable GetOrders(string status = null, DateTime? from = null, DateTime? to = null, string searchTerm = null)
        {
            var prms = new List<SqlParameter>();
            string sql = @"
                SELECT o.OnlineOrderID,
                       ISNULL(o.OrderNumber, '#' + CAST(o.OnlineOrderID AS NVARCHAR(20))) AS OrderNumber,
                       o.RemoteOrderID,
                       o.OrderDate,
                       o.CustomerName,
                       o.CustomerPhone,
                       ISNULL(o.CustomerAddress, '') AS CustomerAddress,
                       ISNULL(o.Notes, '') AS Notes,
                       ISNULL(o.SubTotal, 0) AS SubTotal,
                       ISNULL(o.DeliveryCharge, 0) AS DeliveryCharge,
                       ISNULL(o.TotalAmount, 0) AS TotalAmount,
                       ISNULL(o.PriceTier, N'قطاعي') AS PriceTier,
                       ISNULL(o.Status, N'جديد') AS Status,
                       o.CreatedSaleID,
                       o.CreatedAt,
                       (SELECT COUNT(*) FROM OnlineOrderItems oi WHERE oi.OnlineOrderID = o.OnlineOrderID) AS ItemsCount
                FROM OnlineOrders o WITH (NOLOCK)
                WHERE 1 = 1 ";

            if (!string.IsNullOrEmpty(status) && status != "الكل")
            {
                sql += " AND o.Status = @status ";
                prms.Add(DbHelper.P("@status", status));
            }

            if (from.HasValue)
            {
                sql += " AND o.OrderDate >= @from ";
                prms.Add(DbHelper.P("@from", from.Value.Date));
            }

            if (to.HasValue)
            {
                sql += " AND o.OrderDate <= @to ";
                prms.Add(DbHelper.P("@to", to.Value.Date.AddDays(1).AddSeconds(-1)));
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                string term = "%" + searchTerm.Trim() + "%";
                sql += " AND (o.CustomerName LIKE @term OR o.CustomerPhone LIKE @term OR o.OrderNumber LIKE @term OR o.CustomerAddress LIKE @term) ";
                prms.Add(DbHelper.P("@term", term));
            }

            sql += " ORDER BY o.OnlineOrderID DESC";

            return DbHelper.Query(sql, prms.ToArray());
        }

        public static DataTable GetOrderItems(int onlineOrderID, bool includeStock = true)
        {
            var dt = DbHelper.Query(@"
                SELECT oi.ItemRowID,
                       oi.OnlineOrderID,
                       oi.ProductID,
                       oi.ProductName,
                       ISNULL(oi.UnitName, ISNULL(p.Unit, N'قطعة')) AS UnitName,
                       oi.Quantity,
                       oi.UnitPrice,
                       oi.TotalPrice,
                       ISNULL(oi.Notes, '') AS Notes,
                       ISNULL(p.ProductCode, '') AS ProductCode
                FROM OnlineOrderItems oi WITH (NOLOCK)
                LEFT JOIN Products p WITH (NOLOCK) ON oi.ProductID = p.ProductID
                WHERE oi.OnlineOrderID = @id
                ORDER BY oi.ItemRowID ASC",
                DbHelper.P("@id", onlineOrderID));

            if (includeStock && dt != null)
            {
                if (!dt.Columns.Contains("AvailableStock"))
                    dt.Columns.Add("AvailableStock", typeof(decimal));
                if (!dt.Columns.Contains("StockStatus"))
                    dt.Columns.Add("StockStatus", typeof(string));

                foreach (DataRow r in dt.Rows)
                {
                    int pid = r["ProductID"] != DBNull.Value ? Convert.ToInt32(r["ProductID"]) : 0;
                    decimal qty = r["Quantity"] != DBNull.Value ? Convert.ToDecimal(r["Quantity"]) : 0m;
                    if (pid > 0)
                    {
                        decimal curStock = InventoryDAL.GetProductStock(pid);
                        r["AvailableStock"] = curStock;
                        if (curStock <= 0)
                            r["StockStatus"] = "❌ غير متوفر";
                        else if (curStock < qty)
                            r["StockStatus"] = "⚠️ عجز بالرصيد";
                        else
                            r["StockStatus"] = "✅ متاح";
                    }
                    else
                    {
                        r["AvailableStock"] = 0m;
                        r["StockStatus"] = "❓ صنف يدوي";
                    }
                }
            }

            return dt;
        }

        public static void UpdateOrderItem(int itemRowID, decimal newQuantity, decimal unitPrice, int orderID)
        {
            decimal total = Math.Round(newQuantity * unitPrice, 2);
            DbHelper.Execute(@"
                UPDATE OnlineOrderItems 
                SET Quantity = @qty, UnitPrice = @price, TotalPrice = @tot 
                WHERE ItemRowID = @rowId",
                DbHelper.P("@qty", newQuantity),
                DbHelper.P("@price", unitPrice),
                DbHelper.P("@tot", total),
                DbHelper.P("@rowId", itemRowID));

            RecalculateOrderTotals(orderID);
        }

        public static void DeleteOrderItem(int itemRowID, int orderID)
        {
            DbHelper.Execute("DELETE FROM OnlineOrderItems WHERE ItemRowID = @rowId", DbHelper.P("@rowId", itemRowID));
            RecalculateOrderTotals(orderID);
        }

        public static void AddOrderItem(int orderID, int? productID, string productName, string unitName, decimal quantity, decimal unitPrice)
        {
            decimal total = Math.Round(quantity * unitPrice, 2);
            DbHelper.Execute(@"
                INSERT INTO OnlineOrderItems (OnlineOrderID, ProductID, ProductName, UnitName, Quantity, UnitPrice, TotalPrice, Notes)
                VALUES (@oid, @pid, @name, @unit, @qty, @price, @tot, NULL)",
                DbHelper.P("@oid", orderID),
                DbHelper.P("@pid", productID.HasValue && productID.Value > 0 ? (object)productID.Value : DBNull.Value),
                DbHelper.P("@name", productName ?? "صنف"),
                DbHelper.P("@unit", (object)unitName ?? DBNull.Value),
                DbHelper.P("@qty", quantity),
                DbHelper.P("@price", unitPrice),
                DbHelper.P("@tot", total));

            RecalculateOrderTotals(orderID);
        }

        public static void RecalculateOrderTotals(int orderID)
        {
            DbHelper.Execute(@"
                UPDATE OnlineOrders
                SET SubTotal = ISNULL((SELECT SUM(TotalPrice) FROM OnlineOrderItems WHERE OnlineOrderID = @id), 0),
                    TotalAmount = ISNULL((SELECT SUM(TotalPrice) FROM OnlineOrderItems WHERE OnlineOrderID = @id), 0) + ISNULL(DeliveryCharge, 0),
                    UpdatedAt = GETDATE()
                WHERE OnlineOrderID = @id",
                DbHelper.P("@id", orderID));
        }

        public static DataRow GetOrderRow(int orderID)
        {
            DataTable dt = DbHelper.Query("SELECT * FROM OnlineOrders WITH (NOLOCK) WHERE OnlineOrderID = @id", DbHelper.P("@id", orderID));
            return (dt != null && dt.Rows.Count > 0) ? dt.Rows[0] : null;
        }

        public static bool OrderExists(string remoteId)
        {
            if (string.IsNullOrEmpty(remoteId)) return false;
            object obj = DbHelper.Scalar("SELECT TOP 1 OnlineOrderID FROM OnlineOrders WITH (NOLOCK) WHERE RemoteOrderID = @rid", DbHelper.P("@rid", remoteId));
            return obj != null && obj != DBNull.Value;
        }

        public static int SaveIncomingOrder(
            string remoteId,
            string orderNum,
            DateTime orderDate,
            string custName,
            string custPhone,
            string custAddress,
            string notes,
            decimal subTotal,
            decimal delivery,
            decimal total,
            string priceTier,
            List<OnlineOrderItemDTO> items)
        {
            if (OrderExists(remoteId)) return 0;

            int newOrderId = 0;

            DbHelper.RunInTransaction((con, trans) =>
            {
                var prms = new[]
                {
                    DbHelper.P("@rid", (object)remoteId ?? DBNull.Value),
                    DbHelper.P("@onum", (object)orderNum ?? DBNull.Value),
                    DbHelper.P("@odate", orderDate),
                    DbHelper.P("@name", custName ?? "عميل أونلاين"),
                    DbHelper.P("@phone", custPhone ?? ""),
                    DbHelper.P("@addr", (object)custAddress ?? DBNull.Value),
                    DbHelper.P("@notes", (object)notes ?? DBNull.Value),
                    DbHelper.P("@sub", subTotal),
                    DbHelper.P("@del", delivery),
                    DbHelper.P("@tot", total),
                    DbHelper.P("@tier", string.IsNullOrEmpty(priceTier) ? "قطاعي" : priceTier),
                    DbHelper.P("@status", "جديد")
                };

                object res = DbHelper.ScalarTrans(trans, @"
                    INSERT INTO OnlineOrders
                    (RemoteOrderID, OrderNumber, OrderDate, CustomerName, CustomerPhone, CustomerAddress, Notes, SubTotal, DeliveryCharge, TotalAmount, PriceTier, Status, CreatedAt, UpdatedAt)
                    VALUES
                    (@rid, @onum, @odate, @name, @phone, @addr, @notes, @sub, @del, @tot, @tier, @status, GETDATE(), GETDATE());
                    SELECT SCOPE_IDENTITY();", prms);

                if (res != null && res != DBNull.Value)
                {
                    newOrderId = Convert.ToInt32(res);
                }

                if (newOrderId > 0 && items != null && items.Count > 0)
                {
                    foreach (var it in items)
                    {
                        DbHelper.ExecuteTrans(trans, @"
                            INSERT INTO OnlineOrderItems
                            (OnlineOrderID, ProductID, ProductName, UnitName, Quantity, UnitPrice, TotalPrice, Notes)
                            VALUES
                            (@oid, @pid, @pname, @uname, @qty, @price, @tot, @notes)",
                            DbHelper.P("@oid", newOrderId),
                            DbHelper.P("@pid", it.ProductID.HasValue && it.ProductID.Value > 0 ? (object)it.ProductID.Value : DBNull.Value),
                            DbHelper.P("@pname", it.ProductName ?? "صنف"),
                            DbHelper.P("@uname", (object)it.UnitName ?? DBNull.Value),
                            DbHelper.P("@qty", it.Quantity),
                            DbHelper.P("@price", it.UnitPrice),
                            DbHelper.P("@tot", it.TotalPrice),
                            DbHelper.P("@notes", (object)it.Notes ?? DBNull.Value));
                    }
                }
            });

            return newOrderId;
        }

        public static void UpdateStatus(int onlineOrderID, string status)
        {
            DbHelper.Execute(@"
                UPDATE OnlineOrders 
                SET Status = @status, UpdatedAt = GETDATE() 
                WHERE OnlineOrderID = @id",
                DbHelper.P("@status", status),
                DbHelper.P("@id", onlineOrderID));
        }

        public static void LinkToSale(int onlineOrderID, int saleID)
        {
            DbHelper.Execute(@"
                UPDATE OnlineOrders 
                SET CreatedSaleID = @saleId, Status = N'مكتمل', UpdatedAt = GETDATE() 
                WHERE OnlineOrderID = @id",
                DbHelper.P("@saleId", saleID),
                DbHelper.P("@id", onlineOrderID));
        }

        public static void GetStats(out int newCount, out int inPrepCount, out int completedCount, out decimal todayTotal)
        {
            newCount = 0;
            inPrepCount = 0;
            completedCount = 0;
            todayTotal = 0m;

            try
            {
                DataTable dt = DbHelper.Query(@"
                    SELECT 
                        ISNULL(SUM(CASE WHEN Status = N'جديد' THEN 1 ELSE 0 END), 0) AS NewOrders,
                        ISNULL(SUM(CASE WHEN Status IN (N'قيد التجهيز', N'جاري التوصيل') THEN 1 ELSE 0 END), 0) AS InPrep,
                        ISNULL(SUM(CASE WHEN Status = N'مكتمل' THEN 1 ELSE 0 END), 0) AS Completed,
                        ISNULL(SUM(CASE WHEN CAST(OrderDate AS DATE) = CAST(GETDATE() AS DATE) AND Status <> N'ملغي' THEN TotalAmount ELSE 0 END), 0) AS TodayTotal
                    FROM OnlineOrders WITH (NOLOCK)");

                if (dt != null && dt.Rows.Count > 0)
                {
                    DataRow r = dt.Rows[0];
                    newCount = Convert.ToInt32(r["NewOrders"]);
                    inPrepCount = Convert.ToInt32(r["InPrep"]);
                    completedCount = Convert.ToInt32(r["Completed"]);
                    todayTotal = Convert.ToDecimal(r["TodayTotal"]);
                }
            }
            catch { }
        }

        public static int GetNewOrdersCount()
        {
            try
            {
                object obj = DbHelper.Scalar("SELECT COUNT(*) FROM OnlineOrders WITH (NOLOCK) WHERE Status = N'جديد'");
                return (obj != null && obj != DBNull.Value) ? Convert.ToInt32(obj) : 0;
            }
            catch { return 0; }
        }

        /// <summary>
        /// تقرير عملاء المتجر الإلكتروني مع إحصائيات الطلبات والمشتريات
        /// </summary>
        public static DataTable GetStoreCustomersReport(DateTime? from = null, DateTime? to = null, string searchTerm = null, string filterType = "الكل", string sortBy = "LastOrderDate")
        {
            var prms = new List<SqlParameter>();
            string sql = @"
                SELECT 
                    ISNULL(o.CustomerPhone, '') AS CustomerPhone,
                    MAX(o.CustomerName) AS CustomerName,
                    MAX(ISNULL(o.CustomerAddress, '')) AS CustomerAddress,
                    COUNT(o.OnlineOrderID) AS TotalOrdersCount,
                    SUM(CASE WHEN o.Status = N'مكتمل' THEN 1 ELSE 0 END) AS CompletedOrdersCount,
                    SUM(CASE WHEN o.Status = N'ملغي' THEN 1 ELSE 0 END) AS CanceledOrdersCount,
                    SUM(CASE WHEN o.Status NOT IN (N'مكتمل', N'ملغي') THEN 1 ELSE 0 END) AS PendingOrdersCount,
                    SUM(ISNULL(o.TotalAmount, 0)) AS TotalAmountSpent,
                    SUM(CASE WHEN o.Status = N'مكتمل' THEN ISNULL(o.TotalAmount, 0) ELSE 0 END) AS CompletedAmountSpent,
                    MIN(o.OrderDate) AS FirstOrderDate,
                    MAX(o.OrderDate) AS LastOrderDate,
                    c.ClientID,
                    c.ClientCode,
                    c.ClientName AS RegisteredClientName,
                    CASE WHEN c.ClientID IS NOT NULL THEN 1 ELSE 0 END AS IsRegisteredClient
                FROM OnlineOrders o WITH (NOLOCK)
                LEFT JOIN Clients c WITH (NOLOCK) ON (
                    (ISNULL(o.CustomerPhone, '') <> '' AND (c.Phone = o.CustomerPhone OR c.Phone2 = o.CustomerPhone))
                )
                WHERE 1 = 1 ";

            if (from.HasValue)
            {
                sql += " AND o.OrderDate >= @from ";
                prms.Add(DbHelper.P("@from", from.Value.Date));
            }

            if (to.HasValue)
            {
                sql += " AND o.OrderDate <= @to ";
                prms.Add(DbHelper.P("@to", to.Value.Date.AddDays(1).AddSeconds(-1)));
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                string term = "%" + searchTerm.Trim() + "%";
                sql += " AND (o.CustomerName LIKE @term OR o.CustomerPhone LIKE @term OR o.CustomerAddress LIKE @term OR c.ClientName LIKE @term) ";
                prms.Add(DbHelper.P("@term", term));
            }

            sql += @" GROUP BY ISNULL(o.CustomerPhone, ''), c.ClientID, c.ClientCode, c.ClientName ";

            // HAVING filters based on client status or registration
            if (filterType == "مسجلين بالمنظومة")
            {
                sql += " HAVING c.ClientID IS NOT NULL ";
            }
            else if (filterType == "غير مسجلين")
            {
                sql += " HAVING c.ClientID IS NULL ";
            }
            else if (filterType == "أكثر من طلب")
            {
                sql += " HAVING COUNT(o.OnlineOrderID) > 1 ";
            }
            else if (filterType == "لديهم طلبات مكتملة")
            {
                sql += " HAVING SUM(CASE WHEN o.Status = N'مكتمل' THEN 1 ELSE 0 END) > 0 ";
            }

            // ORDER BY
            switch (sortBy)
            {
                case "TotalSpent":
                    sql += " ORDER BY TotalAmountSpent DESC";
                    break;
                case "TotalOrders":
                    sql += " ORDER BY TotalOrdersCount DESC";
                    break;
                case "Name":
                    sql += " ORDER BY CustomerName ASC";
                    break;
                case "LastOrderDate":
                default:
                    sql += " ORDER BY LastOrderDate DESC";
                    break;
            }

            return DbHelper.Query(sql, prms.ToArray());
        }

        /// <summary>
        /// جلب كافة طلبات عميل متجر محدد بواسطة رقم هاتفه
        /// </summary>
        public static DataTable GetCustomerOrders(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return new DataTable();

            return DbHelper.Query(@"
                SELECT o.OnlineOrderID,
                       ISNULL(o.OrderNumber, '#' + CAST(o.OnlineOrderID AS NVARCHAR(20))) AS OrderNumber,
                       o.OrderDate,
                       o.CustomerName,
                       o.CustomerPhone,
                       ISNULL(o.CustomerAddress, '') AS CustomerAddress,
                       ISNULL(o.Notes, '') AS Notes,
                       ISNULL(o.SubTotal, 0) AS SubTotal,
                       ISNULL(o.DeliveryCharge, 0) AS DeliveryCharge,
                       ISNULL(o.TotalAmount, 0) AS TotalAmount,
                       ISNULL(o.PriceTier, N'قطاعي') AS PriceTier,
                       ISNULL(o.Status, N'جديد') AS Status,
                       o.CreatedSaleID,
                       o.CreatedAt,
                       (SELECT COUNT(*) FROM OnlineOrderItems oi WHERE oi.OnlineOrderID = o.OnlineOrderID) AS ItemsCount
                FROM OnlineOrders o WITH (NOLOCK)
                WHERE o.CustomerPhone = @phone
                ORDER BY o.OnlineOrderID DESC",
                DbHelper.P("@phone", phone.Trim()));
        }

        /// <summary>
        /// تسجيل عميل متجر كعميل رسمي في جدول العملاء Clients
        /// </summary>
        public static int RegisterStoreCustomerAsClient(string name, string phone, string address, out string error)
        {
            error = null;
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    error = "يرجى كتابة اسم العميل";
                    return 0;
                }
                if (string.IsNullOrWhiteSpace(phone))
                {
                    error = "يرجى تحديد رقم الهاتف للعميل";
                    return 0;
                }

                if (ClientDAL.IsDuplicatePhone(phone))
                {
                    error = "رقم الهاتف مسجل بالفعل لعميل آخر بالمنظومة!";
                    return 0;
                }

                string nextCode = ClientDAL.GetNextClientCode();
                int newId = ClientDAL.Save(
                    id: 0,
                    code: nextCode,
                    name: name.Trim(),
                    phone: phone.Trim(),
                    phone2: "",
                    address: address?.Trim() ?? "",
                    opening: 0m,
                    active: true,
                    driverID: null,
                    maxCreditLimit: 0m,
                    notes: "عميل تم تسجيله تلقائياً من تقرير عملاء المتجر الإلكتروني",
                    defaultPriceTier: "قطاعي",
                    openingCrates: 0,
                    defaultPaymentType: "Any"
                );

                return newId;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return 0;
            }
        }
    }
}

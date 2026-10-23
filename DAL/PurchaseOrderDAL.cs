using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using ChickenDist.Core;

namespace ChickenDist.DAL
{
    public class PurchaseOrderDTO
    {
        public int OrderID { get; set; }
        public string OrderCode { get; set; } = "";
        public DateTime OrderDate { get; set; } = DateTime.Now;
        public int SupplierID { get; set; }
        public string SupplierName { get; set; } = "";
        public string SupplierPhone { get; set; } = "";
        public DateTime? ExpectedDeliveryDate { get; set; }
        public string Status { get; set; } = "معلقة"; // معلقة, تم التوريد بالكامل, تم التوريد جزئياً, ملغاة
        public decimal TotalEstimatedCost { get; set; }
        public int TotalItemsCount { get; set; }
        public decimal TotalQuantity { get; set; }
        public int? CreatedByEmpID { get; set; }
        public string CreatedByEmpName { get; set; } = "";
        public int? ConvertedToPurchaseID { get; set; }
        public DateTime? ConvertedDate { get; set; }
        public string Notes { get; set; } = "";
        public List<PurchaseOrderItemDTO> Items { get; set; } = new List<PurchaseOrderItemDTO>();
    }

    public class PurchaseOrderItemDTO
    {
        public int OrderItemID { get; set; }
        public int OrderID { get; set; }
        public int ProductID { get; set; }
        public string ProductCode { get; set; } = "";
        public string ProductName { get; set; } = "";
        public string CategoryName { get; set; } = "";
        public string UnitName { get; set; } = "قطعة";
        public decimal Quantity { get; set; } = 1m;
        public decimal EstimatedUnitPrice { get; set; }
        public decimal TotalPrice => Math.Round(Quantity * EstimatedUnitPrice, 2);
        public decimal CurrentStockAtOrder { get; set; }
        public decimal ReceivedQuantity { get; set; }
        public string Notes { get; set; } = "";
    }

    public class SuggestedPurchaseItemDTO
    {
        public int ProductID { get; set; }
        public string ProductCode { get; set; } = "";
        public string ProductName { get; set; } = "";
        public string CategoryName { get; set; } = "";
        public string UnitName { get; set; } = "قطعة";
        public decimal CurrentStock { get; set; }
        public decimal MinStockLimit { get; set; }
        public decimal LastPurchasePrice { get; set; }
        public decimal SuggestedQuantity { get; set; } = 1m;
        public decimal SoldQtyInPeriod { get; set; }
        public decimal DailyRunRate { get; set; }
        public string SuggestionReason { get; set; } = "";
        public int? LastSupplierID { get; set; }
        public string LastSupplierName { get; set; } = "";
    }

    public static class PurchaseOrderDAL
    {
        public static void EnsureSchema()
        {
            try
            {
                DbHelper.Execute(@"
                    IF OBJECT_ID('PurchaseOrders', 'U') IS NULL
                    BEGIN
                        CREATE TABLE PurchaseOrders (
                            OrderID INT IDENTITY(1,1) PRIMARY KEY,
                            OrderCode NVARCHAR(50) NOT NULL,
                            OrderDate DATETIME NOT NULL DEFAULT GETDATE(),
                            SupplierID INT NOT NULL,
                            SupplierName NVARCHAR(150) NOT NULL,
                            SupplierPhone NVARCHAR(50) NULL,
                            ExpectedDeliveryDate DATETIME NULL,
                            Status NVARCHAR(50) NOT NULL DEFAULT N'معلقة',
                            TotalEstimatedCost DECIMAL(18,2) NOT NULL DEFAULT 0,
                            TotalItemsCount INT NOT NULL DEFAULT 0,
                            TotalQuantity DECIMAL(18,3) NOT NULL DEFAULT 0,
                            CreatedByEmpID INT NULL,
                            CreatedByEmpName NVARCHAR(100) NULL,
                            ConvertedToPurchaseID INT NULL,
                            ConvertedDate DATETIME NULL,
                            Notes NVARCHAR(500) NULL
                        );
                        CREATE INDEX IX_PurchaseOrders_Supplier ON PurchaseOrders(SupplierID, Status, OrderDate DESC);
                        CREATE INDEX IX_PurchaseOrders_Status ON PurchaseOrders(Status, OrderDate DESC);
                    END;

                    IF OBJECT_ID('PurchaseOrderItems', 'U') IS NULL
                    BEGIN
                        CREATE TABLE PurchaseOrderItems (
                            OrderItemID INT IDENTITY(1,1) PRIMARY KEY,
                            OrderID INT NOT NULL FOREIGN KEY REFERENCES PurchaseOrders(OrderID) ON DELETE CASCADE,
                            ProductID INT NOT NULL,
                            ProductCode NVARCHAR(100) NULL,
                            ProductName NVARCHAR(250) NOT NULL,
                            UnitName NVARCHAR(50) NULL,
                            Quantity DECIMAL(18,3) NOT NULL DEFAULT 1,
                            EstimatedUnitPrice DECIMAL(18,2) NOT NULL DEFAULT 0,
                            TotalPrice DECIMAL(18,2) NOT NULL DEFAULT 0,
                            CurrentStockAtOrder DECIMAL(18,3) NULL,
                            ReceivedQuantity DECIMAL(18,3) NOT NULL DEFAULT 0,
                            Notes NVARCHAR(250) NULL
                        );
                        CREATE INDEX IX_PurchaseOrderItems_Order ON PurchaseOrderItems(OrderID);
                        CREATE INDEX IX_PurchaseOrderItems_Product ON PurchaseOrderItems(ProductID);
                    END;");
            }
            catch (Exception ex)
            {
                AppLogger.Error("PurchaseOrderDAL.EnsureSchema", ex);
            }
        }

        public static string GenerateNextOrderCode()
        {
            EnsureSchema();
            try
            {
                var dtNow = DateTime.Now;
                string prefix = $"PO-{dtNow:yyMM}-";
                object maxObj = DbHelper.Scalar("SELECT TOP 1 OrderCode FROM PurchaseOrders WHERE OrderCode LIKE @prefix ORDER BY OrderID DESC", DbHelper.P("@prefix", prefix + "%"));
                int nextSeq = 1;
                if (maxObj != null && maxObj != DBNull.Value)
                {
                    string lastCode = maxObj.ToString();
                    string numPart = lastCode.Substring(prefix.Length);
                    if (int.TryParse(numPart, out int parsed))
                        nextSeq = parsed + 1;
                }
                return $"{prefix}{nextSeq:D3}";
            }
            catch
            {
                return $"PO-{DateTime.Now:yyyyMMdd-HHmm}";
            }
        }

        public static int SaveOrder(PurchaseOrderDTO order)
        {
            EnsureSchema();
            if (order == null) throw new ArgumentNullException(nameof(order));
            if (order.SupplierID <= 0) throw new ArgumentException("يجب تحديد المورد.");
            if (order.Items == null || order.Items.Count == 0) throw new ArgumentException("يجب إضافة صنف واحد على الأقل للطلبية.");

            int orderId = order.OrderID;

            // حساب الإجماليات
            order.TotalItemsCount = order.Items.Count;
            order.TotalQuantity = order.Items.Sum(i => i.Quantity);
            order.TotalEstimatedCost = order.Items.Sum(i => i.Quantity * i.EstimatedUnitPrice);

            if (string.IsNullOrWhiteSpace(order.OrderCode))
                order.OrderCode = GenerateNextOrderCode();

            DbHelper.RunInTransaction((con, trans) =>
            {
                if (orderId <= 0)
                {
                    // إدراج رأس الطلبية
                    string insertSql = @"
                        INSERT INTO PurchaseOrders (
                            OrderCode, OrderDate, SupplierID, SupplierName, SupplierPhone,
                            ExpectedDeliveryDate, Status, TotalEstimatedCost, TotalItemsCount, TotalQuantity,
                            CreatedByEmpID, CreatedByEmpName, Notes
                        ) VALUES (
                            @code, @date, @supId, @supName, @phone,
                            @delivDate, @status, @totalCost, @itemsCount, @totalQty,
                            @empId, @empName, @notes
                        );
                        SELECT SCOPE_IDENTITY();";

                    object sIdObj = DbHelper.ScalarTrans(trans, insertSql,
                        DbHelper.P("@code", order.OrderCode),
                        DbHelper.P("@date", order.OrderDate),
                        DbHelper.P("@supId", order.SupplierID),
                        DbHelper.P("@supName", order.SupplierName ?? ""),
                        DbHelper.P("@phone", order.SupplierPhone ?? ""),
                        DbHelper.P("@delivDate", (object)order.ExpectedDeliveryDate ?? DBNull.Value),
                        DbHelper.P("@status", string.IsNullOrWhiteSpace(order.Status) ? "معلقة" : order.Status),
                        DbHelper.P("@totalCost", order.TotalEstimatedCost),
                        DbHelper.P("@itemsCount", order.TotalItemsCount),
                        DbHelper.P("@totalQty", order.TotalQuantity),
                        DbHelper.P("@empId", (object)order.CreatedByEmpID ?? Session.EmpID),
                        DbHelper.P("@empName", string.IsNullOrWhiteSpace(order.CreatedByEmpName) ? Session.EmpName : order.CreatedByEmpName),
                        DbHelper.P("@notes", order.Notes ?? "")
                    );

                    orderId = Convert.ToInt32(sIdObj);
                    order.OrderID = orderId;
                }
                else
                {
                    // تحديث رأس الطلبية
                    string updateSql = @"
                        UPDATE PurchaseOrders SET
                            SupplierID = @supId,
                            SupplierName = @supName,
                            SupplierPhone = @phone,
                            ExpectedDeliveryDate = @delivDate,
                            Status = @status,
                            TotalEstimatedCost = @totalCost,
                            TotalItemsCount = @itemsCount,
                            TotalQuantity = @totalQty,
                            Notes = @notes
                        WHERE OrderID = @id";

                    DbHelper.ExecuteTrans(trans, updateSql,
                        DbHelper.P("@id", orderId),
                        DbHelper.P("@supId", order.SupplierID),
                        DbHelper.P("@supName", order.SupplierName ?? ""),
                        DbHelper.P("@phone", order.SupplierPhone ?? ""),
                        DbHelper.P("@delivDate", (object)order.ExpectedDeliveryDate ?? DBNull.Value),
                        DbHelper.P("@status", string.IsNullOrWhiteSpace(order.Status) ? "معلقة" : order.Status),
                        DbHelper.P("@totalCost", order.TotalEstimatedCost),
                        DbHelper.P("@itemsCount", order.TotalItemsCount),
                        DbHelper.P("@totalQty", order.TotalQuantity),
                        DbHelper.P("@notes", order.Notes ?? "")
                    );

                    // حذف بنود الطلبية القديمة لإعادة إدخالها محدثة
                    DbHelper.ExecuteTrans(trans, "DELETE FROM PurchaseOrderItems WHERE OrderID = @id", DbHelper.P("@id", orderId));
                }

                // إدراج بنود الطلبية
                string itemSql = @"
                    INSERT INTO PurchaseOrderItems (
                        OrderID, ProductID, ProductCode, ProductName, UnitName,
                        Quantity, EstimatedUnitPrice, TotalPrice, CurrentStockAtOrder, ReceivedQuantity, Notes
                    ) VALUES (
                        @oid, @pid, @code, @name, @unit,
                        @qty, @price, @total, @stock, 0, @notes
                    )";

                foreach (var item in order.Items)
                {
                    DbHelper.ExecuteTrans(trans, itemSql,
                        DbHelper.P("@oid", orderId),
                        DbHelper.P("@pid", item.ProductID),
                        DbHelper.P("@code", item.ProductCode ?? ""),
                        DbHelper.P("@name", item.ProductName ?? ""),
                        DbHelper.P("@unit", string.IsNullOrWhiteSpace(item.UnitName) ? "قطعة" : item.UnitName),
                        DbHelper.P("@qty", item.Quantity),
                        DbHelper.P("@price", item.EstimatedUnitPrice),
                        DbHelper.P("@total", item.TotalPrice),
                        DbHelper.P("@stock", item.CurrentStockAtOrder),
                        DbHelper.P("@notes", item.Notes ?? "")
                    );
                }
            });

            return orderId;
        }

        public static PurchaseOrderDTO GetOrder(int orderID)
        {
            EnsureSchema();
            var dt = DbHelper.Query("SELECT * FROM PurchaseOrders WHERE OrderID = @id", DbHelper.P("@id", orderID));
            if (dt.Rows.Count == 0) return null;

            var r = dt.Rows[0];
            var order = new PurchaseOrderDTO
            {
                OrderID = Convert.ToInt32(r["OrderID"]),
                OrderCode = r["OrderCode"].ToString(),
                OrderDate = Convert.ToDateTime(r["OrderDate"]),
                SupplierID = Convert.ToInt32(r["SupplierID"]),
                SupplierName = r["SupplierName"].ToString(),
                SupplierPhone = r["SupplierPhone"]?.ToString() ?? "",
                ExpectedDeliveryDate = r["ExpectedDeliveryDate"] != DBNull.Value ? Convert.ToDateTime(r["ExpectedDeliveryDate"]) : (DateTime?)null,
                Status = r["Status"].ToString(),
                TotalEstimatedCost = Convert.ToDecimal(r["TotalEstimatedCost"]),
                TotalItemsCount = Convert.ToInt32(r["TotalItemsCount"]),
                TotalQuantity = Convert.ToDecimal(r["TotalQuantity"]),
                CreatedByEmpID = r["CreatedByEmpID"] != DBNull.Value ? Convert.ToInt32(r["CreatedByEmpID"]) : (int?)null,
                CreatedByEmpName = r["CreatedByEmpName"]?.ToString() ?? "",
                ConvertedToPurchaseID = r["ConvertedToPurchaseID"] != DBNull.Value ? Convert.ToInt32(r["ConvertedToPurchaseID"]) : (int?)null,
                ConvertedDate = r["ConvertedDate"] != DBNull.Value ? Convert.ToDateTime(r["ConvertedDate"]) : (DateTime?)null,
                Notes = r["Notes"]?.ToString() ?? ""
            };

            order.Items = GetOrderItems(orderID);
            return order;
        }

        public static List<PurchaseOrderItemDTO> GetOrderItems(int orderID)
        {
            EnsureSchema();
            var list = new List<PurchaseOrderItemDTO>();
            var dt = DbHelper.Query(@"
                SELECT poi.*, COALESCE(c.CategoryName, N'عام') AS CategoryName
                FROM PurchaseOrderItems poi
                LEFT JOIN Products p ON poi.ProductID = p.ProductID
                LEFT JOIN Categories c ON p.CategoryID = c.CategoryID
                WHERE poi.OrderID = @oid
                ORDER BY poi.OrderItemID ASC",
                DbHelper.P("@oid", orderID));

            foreach (DataRow r in dt.Rows)
            {
                list.Add(new PurchaseOrderItemDTO
                {
                    OrderItemID = Convert.ToInt32(r["OrderItemID"]),
                    OrderID = Convert.ToInt32(r["OrderID"]),
                    ProductID = Convert.ToInt32(r["ProductID"]),
                    ProductCode = r["ProductCode"]?.ToString() ?? "",
                    ProductName = r["ProductName"]?.ToString() ?? "",
                    CategoryName = r["CategoryName"]?.ToString() ?? "عام",
                    UnitName = r["UnitName"]?.ToString() ?? "قطعة",
                    Quantity = Convert.ToDecimal(r["Quantity"]),
                    EstimatedUnitPrice = Convert.ToDecimal(r["EstimatedUnitPrice"]),
                    CurrentStockAtOrder = r["CurrentStockAtOrder"] != DBNull.Value ? Convert.ToDecimal(r["CurrentStockAtOrder"]) : 0m,
                    ReceivedQuantity = r["ReceivedQuantity"] != DBNull.Value ? Convert.ToDecimal(r["ReceivedQuantity"]) : 0m,
                    Notes = r["Notes"]?.ToString() ?? ""
                });
            }
            return list;
        }

        public static DataTable GetOrdersList(DateTime? from = null, DateTime? to = null, int? supplierID = null, string status = null, string search = null)
        {
            EnsureSchema();
            var sb = new StringBuilder(@"
                SELECT 
                    po.OrderID,
                    po.OrderCode,
                    po.OrderDate,
                    po.SupplierID,
                    po.SupplierName,
                    po.SupplierPhone,
                    po.ExpectedDeliveryDate,
                    po.Status,
                    po.TotalItemsCount,
                    po.TotalQuantity,
                    po.TotalEstimatedCost,
                    po.CreatedByEmpName,
                    po.ConvertedToPurchaseID,
                    po.ConvertedDate,
                    po.Notes
                FROM PurchaseOrders po
                WHERE 1=1 ");

            var prms = new List<SqlParameter>();

            if (from.HasValue)
            {
                sb.Append(" AND CAST(po.OrderDate AS DATE) >= @from");
                prms.Add(DbHelper.P("@from", from.Value.Date));
            }
            if (to.HasValue)
            {
                sb.Append(" AND CAST(po.OrderDate AS DATE) <= @to");
                prms.Add(DbHelper.P("@to", to.Value.Date));
            }
            if (supplierID.HasValue && supplierID.Value > 0)
            {
                sb.Append(" AND po.SupplierID = @supId");
                prms.Add(DbHelper.P("@supId", supplierID.Value));
            }
            if (!string.IsNullOrWhiteSpace(status) && status != "الكل")
            {
                sb.Append(" AND po.Status = @st");
                prms.Add(DbHelper.P("@st", status.Trim()));
            }
            if (!string.IsNullOrWhiteSpace(search))
            {
                sb.Append(" AND (po.OrderCode LIKE @search OR po.SupplierName LIKE @search OR po.Notes LIKE @search)");
                prms.Add(DbHelper.P("@search", "%" + search.Trim() + "%"));
            }

            sb.Append(" ORDER BY po.OrderID DESC");

            return DbHelper.Query(sb.ToString(), prms.ToArray());
        }

        public static DataTable GetPendingOrders(int? supplierID = null)
        {
            EnsureSchema();
            string sql = @"
                SELECT 
                    po.OrderID,
                    po.OrderCode,
                    po.OrderDate,
                    po.SupplierID,
                    po.SupplierName,
                    po.SupplierPhone,
                    po.ExpectedDeliveryDate,
                    po.Status,
                    po.TotalItemsCount,
                    po.TotalQuantity,
                    po.TotalEstimatedCost,
                    po.Notes
                FROM PurchaseOrders po
                WHERE po.Status IN (N'معلقة', N'تم التوريد جزئياً') ";

            var prms = new List<SqlParameter>();
            if (supplierID.HasValue && supplierID.Value > 0)
            {
                sql += " AND po.SupplierID = @supId ";
                prms.Add(DbHelper.P("@supId", supplierID.Value));
            }

            sql += " ORDER BY po.OrderID DESC";
            return DbHelper.Query(sql, prms.ToArray());
        }

        public static bool MarkAsCompleted(int orderID, int purchaseID)
        {
            EnsureSchema();
            try
            {
                DbHelper.Execute(@"
                    UPDATE PurchaseOrders 
                    SET Status = N'تم التوريد بالكامل',
                        ConvertedToPurchaseID = @pid,
                        ConvertedDate = GETDATE()
                    WHERE OrderID = @oid",
                    DbHelper.P("@pid", purchaseID),
                    DbHelper.P("@oid", orderID));
                return true;
            }
            catch (Exception ex)
            {
                AppLogger.Error("PurchaseOrderDAL.MarkAsCompleted", ex);
                return false;
            }
        }

        public static bool CancelOrder(int orderID)
        {
            EnsureSchema();
            try
            {
                DbHelper.Execute("UPDATE PurchaseOrders SET Status = N'ملغاة' WHERE OrderID = @oid", DbHelper.P("@oid", orderID));
                return true;
            }
            catch (Exception ex)
            {
                AppLogger.Error("PurchaseOrderDAL.CancelOrder", ex);
                return false;
            }
        }

        public static bool DeleteOrder(int orderID)
        {
            EnsureSchema();
            try
            {
                DbHelper.Execute("DELETE FROM PurchaseOrders WHERE OrderID = @oid", DbHelper.P("@oid", orderID));
                return true;
            }
            catch (Exception ex)
            {
                AppLogger.Error("PurchaseOrderDAL.DeleteOrder", ex);
                return false;
            }
        }

        /// <summary>
        /// المحرك الذكي لاقتراح أصناف أمر الشراء بناءً على الرصيد، معدل سرعة البيع، وحركات المورد
        /// </summary>
        public static List<SuggestedPurchaseItemDTO> GetSmartSuggestedProducts(
            int? supplierID = null,
            decimal? maxStockThreshold = null,
            bool fastMovingOnly = false,
            int salesDays = 14,
            bool includeShortages = true,
            int maxResults = 200)
        {
            EnsureSchema();
            var list = new List<SuggestedPurchaseItemDTO>();
            if (salesDays <= 0) salesDays = 14;

            try
            {
                bool hasShortages = false;
                if (includeShortages)
                {
                    try
                    {
                        var chk = DbHelper.Scalar("SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ShortageNotebook'");
                        hasShortages = (chk != null && chk != DBNull.Value);
                    }
                    catch { }
                }

                var prms = new List<SqlParameter>();
                prms.Add(DbHelper.P("@salesDays", salesDays));
                prms.Add(DbHelper.P("@maxResults", maxResults <= 0 ? 200 : maxResults));

                string shortageJoin = hasShortages
                    ? @"OUTER APPLY (
                        SELECT TOP 1 snItem.RequestedQty 
                        FROM ShortageNotebook snItem
                        WHERE snItem.ProductID = p.ProductID 
                          AND snItem.Status IN (N'جديد', N'تم الطلب') 
                        ORDER BY snItem.ShortageID DESC
                    ) sn"
                    : @"OUTER APPLY (
                        SELECT CAST(NULL AS DECIMAL(18,2)) AS RequestedQty
                    ) sn";

                string sql = $@"
                    SELECT TOP (@maxResults)
                        p.ProductID,
                        p.ProductCode,
                        p.ProductName,
                        COALESCE(c.CategoryName, N'عام') AS CategoryName,
                        COALESCE(p.Unit, N'قطعة') AS UnitName,
                        COALESCE(p.MinStockLimit, 0) AS MinStockLimit,
                        COALESCE(p.PurchasePrice, 0) AS PurchasePrice,
                        ISNULL(stk.TotalStock, 0) AS CurrentStock,
                        ISNULL(sales.TotalSold, 0) AS TotalSoldInPeriod,
                        ISNULL(sales.TotalSold, 0) / CAST(@salesDays AS DECIMAL(18,2)) AS DailyRunRate,
                        lastPur.SupplierID AS LastSupplierID,
                        lastPur.SupplierName AS LastSupplierName,
                        sn.RequestedQty AS ShortageRequestedQty
                    FROM Products p
                    LEFT JOIN Categories c ON p.CategoryID = c.CategoryID
                    OUTER APPLY (
                        SELECT ISNULL((SELECT SUM(ps.Quantity) FROM ProductStock ps WHERE ps.ProductID = p.ProductID), 0) AS TotalStock
                    ) stk
                    OUTER APPLY (
                        SELECT 
                            SUM(si.Quantity) AS TotalSold
                        FROM SaleItems si
                        INNER JOIN Sales s ON si.SaleID = s.SaleID
                        WHERE si.ProductID = p.ProductID 
                          AND s.SaleDate >= DATEADD(day, -@salesDays, GETDATE())
                          AND (s.IsReturn IS NULL OR s.IsReturn = 0)
                    ) sales
                    OUTER APPLY (
                        SELECT TOP 1 pu.SupplierID, sup.SupplierName, pi.UnitPrice
                        FROM PurchaseItems pi
                        INNER JOIN Purchases pu ON pi.PurchaseID = pu.PurchaseID
                        LEFT JOIN Suppliers sup ON pu.SupplierID = sup.SupplierID
                        WHERE pi.ProductID = p.ProductID
                        ORDER BY pu.PurchaseDate DESC, pu.PurchaseID DESC
                    ) lastPur
                    {shortageJoin}
                    WHERE p.IsActive = 1 ";

                // تصفية المورد إن وُجد
                if (supplierID.HasValue && supplierID.Value > 0)
                {
                    sql += " AND lastPur.SupplierID = @supId ";
                    prms.Add(DbHelper.P("@supId", supplierID.Value));
                }

                // شروط النقص ومعدل المبيعات
                if (fastMovingOnly)
                {
                    // أصناف سريعة البيع: مبيعاتها اليومية ستؤدي لنفاد رصيدها خلال 7 أيام أو نفدت بالفعل
                    sql += @" AND ISNULL(sales.TotalSold, 0) > 0 
                              AND (
                                  ISNULL(stk.TotalStock, 0) <= 0 
                                  OR ISNULL(stk.TotalStock, 0) <= (ISNULL(sales.TotalSold, 0) / CAST(@salesDays AS DECIMAL(18,2)) * 7)
                              ) ";
                }
                else if (maxStockThreshold.HasValue)
                {
                    sql += " AND ISNULL(stk.TotalStock, 0) <= @maxThreshold ";
                    prms.Add(DbHelper.P("@maxThreshold", maxStockThreshold.Value));
                }
                else
                {
                    // الافتراضي الذكي: رصيد صفر، أو تحت حد الطلب، أو مسجل بنواقص
                    sql += @" AND (
                                ISNULL(stk.TotalStock, 0) <= 0 
                                OR (p.MinStockLimit > 0 AND ISNULL(stk.TotalStock, 0) <= p.MinStockLimit)
                                OR sn.ProductID IS NOT NULL
                            ) ";
                }

                // ترتيب النتائج: الأصناف الأكثر مبيعاً ونفاداً أولاً
                sql += " ORDER BY ISNULL(sales.TotalSold, 0) DESC, ISNULL(stk.TotalStock, 0) ASC ";

                var dt = DbHelper.Query(sql, prms.ToArray());
                foreach (DataRow r in dt.Rows)
                {
                    decimal stock = Convert.ToDecimal(r["CurrentStock"]);
                    decimal minLimit = Convert.ToDecimal(r["MinStockLimit"]);
                    decimal soldQty = Convert.ToDecimal(r["TotalSoldInPeriod"]);
                    decimal runRate = Convert.ToDecimal(r["DailyRunRate"]);
                    decimal buyPrice = Convert.ToDecimal(r["PurchasePrice"]);

                    decimal suggested = 1m;
                    string reason = "";

                    // حساب الكمية المقترحة بدقة
                    if (runRate > 0)
                    {
                        // تغطية استهلاك 14 يوماً قادمة
                        decimal targetCover = Math.Ceiling(runRate * 14m);
                        suggested = Math.Max(1m, targetCover - stock);
                        reason = $"معدل بيع عالي ({soldQty:N0} قطعة آخر {salesDays} يوم)";
                    }
                    else if (stock <= 0)
                    {
                        suggested = minLimit > 0 ? minLimit : 5m;
                        reason = "الرصيد نفد بالكامل (صفر)";
                    }
                    else if (minLimit > 0 && stock <= minLimit)
                    {
                        suggested = Math.Max(1m, (minLimit * 1.5m) - stock);
                        reason = $"وصل لحد الطلب (الرصيد: {stock:N0} / الحد: {minLimit:N0})";
                    }
                    else
                    {
                        suggested = 1m;
                        reason = "أصناف نواقص / طلب توريد";
                    }

                    if (r["ShortageRequestedQty"] != DBNull.Value)
                    {
                        decimal shortReq = Convert.ToDecimal(r["ShortageRequestedQty"]);
                        if (shortReq > 0) suggested = shortReq;
                    }

                    list.Add(new SuggestedPurchaseItemDTO
                    {
                        ProductID = Convert.ToInt32(r["ProductID"]),
                        ProductCode = r["ProductCode"]?.ToString() ?? "",
                        ProductName = r["ProductName"]?.ToString() ?? "",
                        CategoryName = r["CategoryName"]?.ToString() ?? "عام",
                        UnitName = r["UnitName"]?.ToString() ?? "قطعة",
                        CurrentStock = stock,
                        MinStockLimit = minLimit,
                        LastPurchasePrice = buyPrice,
                        SuggestedQuantity = Math.Ceiling(suggested),
                        SoldQtyInPeriod = soldQty,
                        DailyRunRate = runRate,
                        SuggestionReason = reason,
                        LastSupplierID = r["LastSupplierID"] != DBNull.Value ? Convert.ToInt32(r["LastSupplierID"]) : (int?)null,
                        LastSupplierName = r["LastSupplierName"]?.ToString() ?? ""
                    });
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("PurchaseOrderDAL.GetSmartSuggestedProducts", ex);
            }

            return list;
        }
    }
}

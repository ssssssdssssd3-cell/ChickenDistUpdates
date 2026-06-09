using System;
using System.Data;
using System.Data.SqlClient;
using System.Collections.Generic;
using ChickenDist.Core;

namespace ChickenDist.DAL
{
    public static class InventoryDAL
    {
        /// <summary>جلب رصيد الجرد الحالي لكل الأصناف مع إمكانية تحديد المخزن والبحث السريع</summary>
        public static DataTable GetStock(int? warehouseID = null, string searchTerm = "")
        {
            string filter = "";
            List<SqlParameter> prms = new List<SqlParameter>();
            if (!string.IsNullOrEmpty(searchTerm))
            {
                filter = " AND (p.ProductName LIKE @term OR p.ProductCode LIKE @term OR p.PartNumber LIKE @term) ";
                prms.Add(DbHelper.P("@term", "%" + searchTerm + "%"));
            }

            if (warehouseID.HasValue)
            {
                prms.Add(DbHelper.P("@wid", warehouseID.Value));
            }

            string sql = $@"
                SELECT 
                    p.ProductID,
                    p.ProductCode,
                    p.PartNumber,
                    p.ProductName,
                    p.Unit,
                    p.SalePrice,
                    p.PurchasePrice,
                    p.MinStockLimit,
                    p.ShelfLocation,
                    SUM(t.StockQty) AS BookQty
                FROM Products p
                LEFT JOIN (
                    SELECT 
                        p2.ProductID,
                        w.WarehouseID,
                        ISNULL(adj.ActualQty, 0) + 
                        -- Incoming since adjustment: Sales Returns
                        ISNULL((SELECT SUM(ri.Quantity) 
                                FROM ReturnItems ri 
                                JOIN SalesReturns sr ON ri.ReturnID = sr.ReturnID 
                                WHERE ri.ProductID = p2.ProductID 
                                  AND (adj.AdjDate IS NULL OR sr.ReturnDate > adj.AdjDate)
                                  AND sr.WarehouseID = w.WarehouseID), 0) +
                        -- Incoming since adjustment: Driver Handover Returns
                        ISNULL((SELECT SUM(hi.ReturnedQty) 
                                FROM HandoverItems hi 
                                JOIN DriverHandovers dh ON hi.HandoverID = dh.HandoverID
                                JOIN DriverLoads dl ON dh.LoadID = dl.LoadID
                                WHERE hi.ProductID = p2.ProductID 
                                  AND (adj.AdjDate IS NULL OR dh.HandoverDate > adj.AdjDate)
                                  AND dl.WarehouseID = w.WarehouseID), 0) +
                        -- Incoming since adjustment: Purchases
                        ISNULL((SELECT SUM(pi.Quantity)
                                FROM PurchaseItems pi
                                JOIN Purchases pu ON pi.PurchaseID = pu.PurchaseID
                                WHERE pi.ProductID = p2.ProductID
                                  AND pu.IsPosted = 1
                                  AND (adj.AdjDate IS NULL OR pu.PurchaseDate > adj.AdjDate)
                                  AND pu.WarehouseID = w.WarehouseID), 0) +
                        -- Incoming since adjustment: Warehouse Transfers
                        ISNULL((SELECT SUM(ti.Quantity)
                                FROM WarehouseTransferItems ti
                                JOIN WarehouseTransfers t ON ti.TransferID = t.TransferID
                                WHERE ti.ProductID = p2.ProductID
                                  AND t.IsPosted = 1
                                  AND (adj.AdjDate IS NULL OR t.TransferDate > adj.AdjDate)
                                  AND t.ToWarehouseID = w.WarehouseID), 0)
                        -- Outgoing since adjustment: Purchase Returns
                        - ISNULL((SELECT SUM(pri.Quantity)
                                  FROM PurchaseReturnItems pri
                                  JOIN PurchaseReturns pr ON pri.ReturnID = pr.ReturnID
                                  WHERE pri.ProductID = p2.ProductID
                                    AND (adj.AdjDate IS NULL OR pr.ReturnDate > adj.AdjDate)
                                    AND pr.WarehouseID = w.WarehouseID), 0)
                        -- Outgoing since adjustment: Warehouse Sales & Driver Loads (prevent double counting driver road sales)
                        - ISNULL((SELECT SUM(si.Quantity) 
                                FROM SaleItems si 
                                JOIN Sales s ON si.SaleID = s.SaleID
                                WHERE si.ProductID = p2.ProductID 
                                  AND s.IsPosted = 1
                                  AND (s.SaleType = 'DriverLoad' OR (s.SaleType IN ('Cash', 'Credit') AND s.DriverID IS NULL))
                                  AND (adj.AdjDate IS NULL OR s.SaleDate > adj.AdjDate)
                                  AND s.WarehouseID = w.WarehouseID), 0)
                        -- Outgoing since adjustment: Warehouse Transfers
                        - ISNULL((SELECT SUM(ti.Quantity)
                                FROM WarehouseTransferItems ti
                                JOIN WarehouseTransfers t ON ti.TransferID = t.TransferID
                                WHERE ti.ProductID = p2.ProductID
                                  AND t.IsPosted = 1
                                  AND (adj.AdjDate IS NULL OR t.TransferDate > adj.AdjDate)
                                  AND t.FromWarehouseID = w.WarehouseID), 0) AS StockQty
                    FROM Products p2
                    CROSS JOIN Warehouses w
                    OUTER APPLY (
                        SELECT TOP 1 sa.AdjDate, sa.ActualQty 
                        FROM StockAdjustments sa 
                        WHERE sa.ProductID = p2.ProductID AND sa.WarehouseID = w.WarehouseID
                        ORDER BY sa.AdjDate DESC
                    ) adj
                    WHERE w.IsActive = 1
                      {(warehouseID.HasValue ? "AND w.WarehouseID = @wid" : "")}
                ) t ON p.ProductID = t.ProductID
                WHERE p.IsActive = 1 {filter}
                GROUP BY 
                    p.ProductID,
                    p.ProductCode,
                    p.PartNumber,
                    p.ProductName,
                    p.Unit,
                    p.SalePrice,
                    p.PurchasePrice,
                    p.MinStockLimit,
                    p.ShelfLocation
                ORDER BY p.ProductName";

            return DbHelper.Query(sql, prms.ToArray());
        }

        /// <summary>جلب رصيد صنف محدد في مخزن معين (أو إجمالي المخازن)</summary>
        public static decimal GetProductStock(int productID, int? warehouseID = null)
        {
            List<SqlParameter> prms = new List<SqlParameter> { DbHelper.P("@pid", productID) };
            if (warehouseID.HasValue)
            {
                prms.Add(DbHelper.P("@wid", warehouseID.Value));
            }

            string sql = $@"
                SELECT ISNULL(SUM(StockQty), 0) FROM (
                    SELECT 
                        ISNULL(adj.ActualQty, 0) + 
                        -- Incoming since adjustment: Sales Returns
                        ISNULL((SELECT SUM(ri.Quantity) 
                                FROM ReturnItems ri 
                                JOIN SalesReturns sr ON ri.ReturnID = sr.ReturnID 
                                WHERE ri.ProductID = p.ProductID 
                                  AND (adj.AdjDate IS NULL OR sr.ReturnDate > adj.AdjDate)
                                  AND sr.WarehouseID = w.WarehouseID), 0) +
                        -- Incoming since adjustment: Driver Handover Returns
                        ISNULL((SELECT SUM(hi.ReturnedQty) 
                                FROM HandoverItems hi 
                                JOIN DriverHandovers dh ON hi.HandoverID = dh.HandoverID
                                JOIN DriverLoads dl ON dh.LoadID = dl.LoadID
                                WHERE hi.ProductID = p.ProductID 
                                  AND (adj.AdjDate IS NULL OR dh.HandoverDate > adj.AdjDate)
                                  AND dl.WarehouseID = w.WarehouseID), 0) +
                        -- Incoming since adjustment: Purchases
                        ISNULL((SELECT SUM(pi.Quantity)
                                FROM PurchaseItems pi
                                JOIN Purchases pu ON pi.PurchaseID = pu.PurchaseID
                                WHERE pi.ProductID = p.ProductID
                                  AND pu.IsPosted = 1
                                  AND (adj.AdjDate IS NULL OR pu.PurchaseDate > adj.AdjDate)
                                  AND pu.WarehouseID = w.WarehouseID), 0) +
                        -- Incoming since adjustment: Warehouse Transfers
                        ISNULL((SELECT SUM(ti.Quantity)
                                FROM WarehouseTransferItems ti
                                JOIN WarehouseTransfers t ON ti.TransferID = t.TransferID
                                WHERE ti.ProductID = p.ProductID
                                  AND t.IsPosted = 1
                                  AND (adj.AdjDate IS NULL OR t.TransferDate > adj.AdjDate)
                                  AND t.ToWarehouseID = w.WarehouseID), 0)
                        -- Outgoing since adjustment: Purchase Returns
                        - ISNULL((SELECT SUM(pri.Quantity)
                                  FROM PurchaseReturnItems pri
                                  JOIN PurchaseReturns pr ON pri.ReturnID = pr.ReturnID
                                  WHERE pri.ProductID = p.ProductID
                                    AND (adj.AdjDate IS NULL OR pr.ReturnDate > adj.AdjDate)
                                    AND pr.WarehouseID = w.WarehouseID), 0)
                        -- Outgoing since adjustment: Warehouse Sales & Driver Loads (prevent double counting driver road sales)
                        - ISNULL((SELECT SUM(si.Quantity) 
                                FROM SaleItems si 
                                JOIN Sales s ON si.SaleID = s.SaleID
                                WHERE si.ProductID = p.ProductID 
                                  AND s.IsPosted = 1
                                  AND (s.SaleType = 'DriverLoad' OR (s.SaleType IN ('Cash', 'Credit') AND s.DriverID IS NULL))
                                  AND (adj.AdjDate IS NULL OR s.SaleDate > adj.AdjDate)
                                  AND s.WarehouseID = w.WarehouseID), 0)
                        -- Outgoing since adjustment: Warehouse Transfers
                        - ISNULL((SELECT SUM(ti.Quantity)
                                FROM WarehouseTransferItems ti
                                JOIN WarehouseTransfers t ON ti.TransferID = t.TransferID
                                WHERE ti.ProductID = p.ProductID
                                  AND t.IsPosted = 1
                                  AND (adj.AdjDate IS NULL OR t.TransferDate > adj.AdjDate)
                                  AND t.FromWarehouseID = w.WarehouseID), 0) AS StockQty
                    FROM Products p
                    CROSS JOIN Warehouses w
                    OUTER APPLY (
                        SELECT TOP 1 sa.AdjDate, sa.ActualQty 
                        FROM StockAdjustments sa 
                        WHERE sa.ProductID = p.ProductID AND sa.WarehouseID = w.WarehouseID
                        ORDER BY sa.AdjDate DESC
                    ) adj
                    WHERE p.ProductID = @pid 
                      AND w.IsActive = 1
                      {(warehouseID.HasValue ? "AND w.WarehouseID = @wid" : "")}
                ) t";

            var val = DbHelper.Scalar(sql, prms.ToArray());
            return val == null || val == DBNull.Value ? 0 : Convert.ToDecimal(val);
        }

        /// <summary>حفظ تسوية جردية لصنف في مخزن محدد</summary>
        public static int SaveAdjustment(int productID, int warehouseID, decimal bookQty, decimal actualQty, string notes)
        {
            return DbHelper.ExecuteInsert(
                @"INSERT INTO StockAdjustments (ProductID, WarehouseID, BookQty, ActualQty, Notes, CreatedBy)
                  VALUES (@pid, @wid, @bq, @aq, @notes, @by)",
                DbHelper.P("@pid", productID),
                DbHelper.P("@wid", warehouseID),
                DbHelper.P("@bq", bookQty),
                DbHelper.P("@aq", actualQty),
                DbHelper.P("@notes", notes),
                DbHelper.P("@by", Session.EmpID)
            );
        }

        /// <summary>جلب تاريخ تسويات الجرد للفترة مع فلترة بالمخزن</summary>
        public static DataTable GetAdjustments(DateTime from, DateTime to, int? warehouseID = null, string searchTerm = "")
        {
            string filter = "";
            List<SqlParameter> prms = new List<SqlParameter> {
                DbHelper.P("@f", from.Date),
                DbHelper.P("@t", to.Date)
            };

            if (!string.IsNullOrEmpty(searchTerm))
            {
                filter += " AND (p.ProductName LIKE @term OR p.ProductCode LIKE @term OR sa.Notes LIKE @term) ";
                prms.Add(DbHelper.P("@term", "%" + searchTerm + "%"));
            }

            if (warehouseID.HasValue)
            {
                filter += " AND sa.WarehouseID = @wid ";
                prms.Add(DbHelper.P("@wid", warehouseID.Value));
            }

            string sql = $@"
                SELECT 
                    sa.AdjID,
                    sa.AdjDate,
                    w.WarehouseName,
                    p.ProductCode,
                    p.ProductName,
                    p.Unit,
                    sa.BookQty,
                    sa.ActualQty,
                    (sa.ActualQty - sa.BookQty) AS DiffQty,
                    sa.Notes,
                    e.EmpName AS CreatedBy
                FROM StockAdjustments sa
                JOIN Products p ON sa.ProductID = p.ProductID
                JOIN Warehouses w ON sa.WarehouseID = w.WarehouseID
                LEFT JOIN Employees e ON sa.CreatedBy = e.EmpID
                WHERE CAST(sa.AdjDate AS DATE) BETWEEN @f AND @t {filter}
                ORDER BY sa.AdjDate DESC";

            return DbHelper.Query(sql, prms.ToArray());
        }

        /// <summary>جلب تقرير حركة الصنف الكامل مفصلة أو مصفاة بمخزن</summary>
        public static DataTable GetProductMovement(int productID, int? warehouseID = null)
        {
            List<SqlParameter> prms = new List<SqlParameter> { DbHelper.P("@pid", productID) };
            if (warehouseID.HasValue)
            {
                prms.Add(DbHelper.P("@wid", warehouseID.Value));
            }

            string whFilterSales = warehouseID.HasValue ? "AND s.WarehouseID = @wid" : "";
            string whFilterReturns = warehouseID.HasValue ? "AND sr.WarehouseID = @wid" : "";
            string whFilterHandovers = warehouseID.HasValue ? "AND dl.WarehouseID = @wid" : "";
            string whFilterAdjustments = warehouseID.HasValue ? "AND sa.WarehouseID = @wid" : "";
            string whFilterPurchases = warehouseID.HasValue ? "AND pu.WarehouseID = @wid" : "";
            string whFilterPurchaseReturns = warehouseID.HasValue ? "AND pr.WarehouseID = @wid" : "";

            string sql = $@"
                SELECT 
                    MovDate AS TransDate,
                    MovType AS TransType,
                    RefCode,
                    PersonName,
                    WarehouseName,
                    QtyIn,
                    QtyOut,
                    Notes
                FROM (
                    -- 1. Direct Sales / Driver Loads (Outgoing)
                    SELECT 
                        s.SaleDate AS MovDate,
                        CASE s.SaleType 
                            WHEN 'Cash' THEN N'بيع نقدي (مستودع)' 
                            WHEN 'Credit' THEN N'بيع آجل (مستودع)' 
                            ELSE N'تحميل حمولة مندوب' 
                        END AS MovType,
                        s.SaleCode AS RefCode,
                        CASE s.SaleType
                            WHEN 'DriverLoad' THEN ISNULL(e.EmpName, N'---')
                            ELSE ISNULL(c.ClientName, N'---')
                        END AS PersonName,
                        w.WarehouseName,
                        0.00 AS QtyIn,
                        si.Quantity AS QtyOut,
                        s.Notes
                    FROM SaleItems si
                    JOIN Sales s ON si.SaleID = s.SaleID
                    JOIN Warehouses w ON s.WarehouseID = w.WarehouseID
                    LEFT JOIN Clients c ON s.ClientID = c.ClientID
                    LEFT JOIN Employees e ON s.DriverID = e.EmpID
                    WHERE si.ProductID = @pid
                      AND s.IsPosted = 1
                      AND (s.SaleType = 'DriverLoad' OR (s.SaleType IN ('Cash', 'Credit') AND s.DriverID IS NULL))
                      {whFilterSales}
 
                    UNION ALL
 
                    -- 2. Sales Returns (Incoming)
                    SELECT 
                        sr.ReturnDate AS MovDate,
                        N'مرتجع مبيعات' AS MovType,
                        ISNULL(s.SaleCode, N'---') AS RefCode,
                        ISNULL(c.ClientName, N'---') AS PersonName,
                        w.WarehouseName,
                        ri.Quantity AS QtyIn,
                        0.00 AS QtyOut,
                        sr.Notes
                    FROM ReturnItems ri
                    JOIN SalesReturns sr ON ri.ReturnID = sr.ReturnID
                    JOIN Warehouses w ON sr.WarehouseID = w.WarehouseID
                    LEFT JOIN Sales s ON sr.SaleID = s.SaleID
                    LEFT JOIN Clients c ON sr.ClientID = c.ClientID
                    WHERE ri.ProductID = @pid
                      {whFilterReturns}
 
                    UNION ALL
 
                    -- 3. Driver Handovers (Incoming returned goods)
                    SELECT 
                        dh.HandoverDate AS MovDate,
                        N'مرتجع حمولة مندوب' AS MovType,
                        ISNULL(s.SaleCode, N'---') AS RefCode,
                        ISNULL(e.EmpName, N'---') AS PersonName,
                        w.WarehouseName,
                        hi.ReturnedQty AS QtyIn,
                        0.00 AS QtyOut,
                        dh.Notes
                    FROM HandoverItems hi
                    JOIN DriverHandovers dh ON hi.HandoverID = dh.HandoverID
                    JOIN DriverLoads dl ON dh.LoadID = dl.LoadID
                    JOIN Warehouses w ON dl.WarehouseID = w.WarehouseID
                    JOIN Sales s ON dl.SaleID = s.SaleID
                    LEFT JOIN Employees e ON dh.DriverID = e.EmpID
                    WHERE hi.ProductID = @pid AND hi.ReturnedQty > 0
                      {whFilterHandovers}
 
                    UNION ALL
 
                    -- 4. Stock Adjustments (Incoming or Outgoing)
                    SELECT 
                        sa.AdjDate AS MovDate,
                        N'تسوية جردية' AS MovType,
                        N'تسوية #' + CAST(sa.AdjID AS NVARCHAR(20)) AS RefCode,
                        ISNULL(e.EmpName, N'---') AS PersonName,
                        w.WarehouseName,
                        CASE WHEN (sa.ActualQty - sa.BookQty) > 0 THEN (sa.ActualQty - sa.BookQty) ELSE 0.00 END AS QtyIn,
                        CASE WHEN (sa.ActualQty - sa.BookQty) < 0 THEN ABS(sa.ActualQty - sa.BookQty) ELSE 0.00 END AS QtyOut,
                        sa.Notes
                    FROM StockAdjustments sa
                    JOIN Warehouses w ON sa.WarehouseID = w.WarehouseID
                    LEFT JOIN Employees e ON sa.CreatedBy = e.EmpID
                    WHERE sa.ProductID = @pid
                      {whFilterAdjustments}
 
                    UNION ALL
 
                    -- 5. Purchases (Incoming)
                    SELECT 
                        pu.PurchaseDate AS MovDate,
                        CASE pu.PurchaseType 
                            WHEN 'Cash' THEN N'شراء نقدي' 
                            WHEN 'Credit' THEN N'شراء آجل' 
                            ELSE N'فاتورة شراء' 
                        END AS MovType,
                        pu.PurchaseCode AS RefCode,
                        ISNULL(sup.SupplierName, N'---') AS PersonName,
                        w.WarehouseName,
                        pi.Quantity AS QtyIn,
                        0.00 AS QtyOut,
                        pu.Notes
                    FROM PurchaseItems pi
                    JOIN Purchases pu ON pi.PurchaseID = pu.PurchaseID
                    JOIN Warehouses w ON pu.WarehouseID = w.WarehouseID
                    LEFT JOIN Suppliers sup ON pu.SupplierID = sup.SupplierID
                    WHERE pi.ProductID = @pid AND pu.IsPosted = 1
                      {whFilterPurchases}
 
                    UNION ALL
 
                    -- 6. Purchase Returns (Outgoing)
                    SELECT 
                        pr.ReturnDate AS MovDate,
                        N'مرتجع مشتريات' AS MovType,
                        N'مرتجع #' + CAST(pr.ReturnID AS NVARCHAR(20)) AS RefCode,
                        ISNULL(sup.SupplierName, N'---') AS PersonName,
                        w.WarehouseName,
                        0.00 AS QtyIn,
                        pri.Quantity AS QtyOut,
                        pr.Notes
                    FROM PurchaseReturnItems pri
                    JOIN PurchaseReturns pr ON pri.ReturnID = pr.ReturnID
                    JOIN Warehouses w ON pr.WarehouseID = w.WarehouseID
                    LEFT JOIN Suppliers sup ON pr.SupplierID = sup.SupplierID
                    WHERE pri.ProductID = @pid
                      {whFilterPurchaseReturns}

                    UNION ALL

                    -- 7. Incoming Transfers (Incoming to this warehouse)
                    SELECT 
                        t.TransferDate AS MovDate,
                        N'تحويل وارد' AS MovType,
                        t.TransferCode AS RefCode,
                        N'من: ' + wFrom.WarehouseName AS PersonName,
                        wTo.WarehouseName,
                        ti.Quantity AS QtyIn,
                        0.00 AS QtyOut,
                        t.Notes
                    FROM WarehouseTransferItems ti
                    JOIN WarehouseTransfers t ON ti.TransferID = t.TransferID
                    JOIN Warehouses wFrom ON t.FromWarehouseID = wFrom.WarehouseID
                    JOIN Warehouses wTo ON t.ToWarehouseID = wTo.WarehouseID
                    WHERE ti.ProductID = @pid AND t.IsPosted = 1
                      {(warehouseID.HasValue ? "AND t.ToWarehouseID = @wid" : "")}

                    UNION ALL

                    -- 8. Outgoing Transfers (Outgoing from this warehouse)
                    SELECT 
                        t.TransferDate AS MovDate,
                        N'تحويل صادر' AS MovType,
                        t.TransferCode AS RefCode,
                        N'إلى: ' + wTo.WarehouseName AS PersonName,
                        wFrom.WarehouseName,
                        0.00 AS QtyIn,
                        ti.Quantity AS QtyOut,
                        t.Notes
                    FROM WarehouseTransferItems ti
                    JOIN WarehouseTransfers t ON ti.TransferID = t.TransferID
                    JOIN Warehouses wFrom ON t.FromWarehouseID = wFrom.WarehouseID
                    JOIN Warehouses wTo ON t.ToWarehouseID = wTo.WarehouseID
                    WHERE ti.ProductID = @pid AND t.IsPosted = 1
                      {(warehouseID.HasValue ? "AND t.FromWarehouseID = @wid" : "")}
                ) AS Movements
                ORDER BY MovDate ASC";

            return DbHelper.Query(sql, prms.ToArray());
        }
    }
}

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
        public static DataTable GetStock(int? warehouseID = null, string searchTerm = "", bool belowMinOnly = false, bool hideZeroStock = false, bool expiryOnly = false, int? categoryID = null, int maxRows = 300)
        {
            var stockDict = GetStockSummary(warehouseID);

            List<SqlParameter> prms = new List<SqlParameter>();
            prms.Add(DbHelper.P("@maxRows", maxRows <= 0 ? 300 : maxRows));

            string whereClause = " WHERE p.IsActive = 1 ";
            if (!string.IsNullOrEmpty(searchTerm))
            {
                whereClause += " AND (p.ProductName LIKE @term OR p.ProductCode LIKE @term OR p.PartNumber LIKE @term) ";
                prms.Add(DbHelper.P("@term", "%" + searchTerm + "%"));
            }

            if (categoryID.HasValue)
            {
                whereClause += " AND p.CategoryID = @catid ";
                prms.Add(DbHelper.P("@catid", categoryID.Value));
            }

            if (expiryOnly)
            {
                whereClause += " AND COALESCE(p.HasExpiry, 0) = 1 ";
            }

            string sql = $@"
                SELECT TOP (@maxRows) 
                    p.ProductID,
                    p.ProductCode,
                    p.PartNumber,
                    p.ProductName,
                    p.Unit,
                    p.SalePrice,
                    p.PurchasePrice,
                    p.MinStockLimit,
                    p.ShelfLocation,
                    p.Unit1Name, p.Unit1Barcode, p.Unit1SalePrice, p.Unit1PurchasePrice,
                    p.Unit2Name, p.Unit2Factor, p.Unit2Barcode, p.Unit2SalePrice, p.Unit2PurchasePrice,
                    p.Unit3Factor, 
                    COALESCE(p.HasExpiry, 0) AS HasExpiry, 
                    p.DefaultExpiryDays, 
                    p.CategoryID,
                    CAST(0.000 AS DECIMAL(18,3)) AS BookQty,
                    p.IsActive,
                    CAST(NULL AS INT) AS BatchID,
                    CAST(NULL AS DATE) AS ExpiryDate
                FROM Products p
                {whereClause}
                ORDER BY p.ProductName";

            DataTable dt = DbHelper.Query(sql, prms.ToArray());
            DataTable result = dt.Clone();

            foreach (DataRow r in dt.Rows)
            {
                int pid = Convert.ToInt32(r["ProductID"]);
                decimal bq = stockDict.TryGetValue(pid, out decimal val) ? val : 0m;
                r["BookQty"] = bq;

                if (belowMinOnly)
                {
                    decimal minLimit = r["MinStockLimit"] != DBNull.Value ? Convert.ToDecimal(r["MinStockLimit"]) : 0m;
                    if (minLimit <= 0 || bq > minLimit) continue;
                }

                if (hideZeroStock && bq == 0m) continue;

                result.ImportRow(r);
            }

            return result;
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
                SELECT 
                    ISNULL(adj.ActualQty * COALESCE(adj.Factor, COALESCE(p.Unit3Factor * p.Unit2Factor, p.Unit3Factor, p.Unit2Factor, 1.0)), 0) + 
                    -- Incoming since adjustment: Sales Returns
                    ISNULL((SELECT SUM(ri.Quantity * ISNULL(ri.Factor, 0)) FROM ReturnItems ri JOIN SalesReturns sr ON ri.ReturnID = sr.ReturnID WHERE ri.ProductID = p.ProductID AND (adj.AdjDate IS NULL OR sr.ReturnDate > adj.AdjDate) {(warehouseID.HasValue ? "AND sr.WarehouseID = @wid" : "")}), 0) +
                    COALESCE(p.Unit3Factor * p.Unit2Factor, p.Unit3Factor, p.Unit2Factor, 1.0) * ISNULL((SELECT SUM(ri.Quantity) FROM ReturnItems ri JOIN SalesReturns sr ON ri.ReturnID = sr.ReturnID WHERE ri.ProductID = p.ProductID AND ri.Factor IS NULL AND (adj.AdjDate IS NULL OR sr.ReturnDate > adj.AdjDate) {(warehouseID.HasValue ? "AND sr.WarehouseID = @wid" : "")}), 0) +
                    -- Incoming since adjustment: Driver Handover Returns
                    ISNULL((SELECT SUM(hi.ReturnedQty * ISNULL(hi.Factor, 0)) FROM HandoverItems hi JOIN DriverHandovers dh ON hi.HandoverID = dh.HandoverID JOIN DriverLoads dl ON dh.LoadID = dl.LoadID WHERE hi.ProductID = p.ProductID AND (adj.AdjDate IS NULL OR dh.HandoverDate > adj.AdjDate) {(warehouseID.HasValue ? "AND dl.WarehouseID = @wid" : "")}), 0) +
                    COALESCE(p.Unit3Factor * p.Unit2Factor, p.Unit3Factor, p.Unit2Factor, 1.0) * ISNULL((SELECT SUM(hi.ReturnedQty) FROM HandoverItems hi JOIN DriverHandovers dh ON hi.HandoverID = dh.HandoverID JOIN DriverLoads dl ON dh.LoadID = dl.LoadID WHERE hi.ProductID = p.ProductID AND hi.Factor IS NULL AND (adj.AdjDate IS NULL OR dh.HandoverDate > adj.AdjDate) {(warehouseID.HasValue ? "AND dl.WarehouseID = @wid" : "")}), 0) +
                    -- Incoming since adjustment: Purchases
                    ISNULL((SELECT SUM(pi.Quantity * ISNULL(pi.Factor, 0)) FROM PurchaseItems pi JOIN Purchases pu ON pi.PurchaseID = pu.PurchaseID WHERE pi.ProductID = p.ProductID AND pu.IsPosted = 1 AND (adj.AdjDate IS NULL OR pu.PurchaseDate > adj.AdjDate) {(warehouseID.HasValue ? "AND pu.WarehouseID = @wid" : "")}), 0) +
                    COALESCE(p.Unit3Factor * p.Unit2Factor, p.Unit3Factor, p.Unit2Factor, 1.0) * ISNULL((SELECT SUM(pi.Quantity) FROM PurchaseItems pi JOIN Purchases pu ON pi.PurchaseID = pu.PurchaseID WHERE pi.ProductID = p.ProductID AND pi.Factor IS NULL AND pu.IsPosted = 1 AND (adj.AdjDate IS NULL OR pu.PurchaseDate > adj.AdjDate) {(warehouseID.HasValue ? "AND pu.WarehouseID = @wid" : "")}), 0) +
                    -- Incoming since adjustment: Warehouse Transfers
                    ISNULL((SELECT SUM(ti.Quantity * ISNULL(ti.Factor, 0)) FROM WarehouseTransferItems ti JOIN WarehouseTransfers t ON ti.TransferID = t.TransferID WHERE ti.ProductID = p.ProductID AND t.IsPosted = 1 AND (adj.AdjDate IS NULL OR t.TransferDate > adj.AdjDate) {(warehouseID.HasValue ? "AND t.ToWarehouseID = @wid" : "")}), 0) +
                    COALESCE(p.Unit3Factor * p.Unit2Factor, p.Unit3Factor, p.Unit2Factor, 1.0) * ISNULL((SELECT SUM(ti.Quantity) FROM WarehouseTransferItems ti JOIN WarehouseTransfers t ON ti.TransferID = t.TransferID WHERE ti.ProductID = p.ProductID AND ti.Factor IS NULL AND t.IsPosted = 1 AND (adj.AdjDate IS NULL OR t.TransferDate > adj.AdjDate) {(warehouseID.HasValue ? "AND t.ToWarehouseID = @wid" : "")}), 0)
                    -- Outgoing since adjustment: Purchase Returns
                    - ISNULL((SELECT SUM(pri.Quantity * ISNULL(pri.Factor, 0)) FROM PurchaseReturnItems pri JOIN PurchaseReturns pr ON pri.ReturnID = pr.ReturnID WHERE pri.ProductID = p.ProductID AND (adj.AdjDate IS NULL OR pr.ReturnDate > adj.AdjDate) {(warehouseID.HasValue ? "AND pr.WarehouseID = @wid" : "")}), 0)
                    - COALESCE(p.Unit3Factor * p.Unit2Factor, p.Unit3Factor, p.Unit2Factor, 1.0) * ISNULL((SELECT SUM(pri.Quantity) FROM PurchaseReturnItems pri JOIN PurchaseReturns pr ON pri.ReturnID = pr.ReturnID WHERE pri.ProductID = p.ProductID AND pri.Factor IS NULL AND (adj.AdjDate IS NULL OR pr.ReturnDate > adj.AdjDate) {(warehouseID.HasValue ? "AND pr.WarehouseID = @wid" : "")}), 0)
                    -- Outgoing since adjustment: Warehouse Sales & Driver Loads (prevent double counting driver road sales)
                    - ISNULL((SELECT SUM(si.Quantity * ISNULL(si.Factor, 0)) FROM SaleItems si JOIN Sales s ON si.SaleID = s.SaleID WHERE si.ProductID = p.ProductID AND s.IsPosted = 1 AND (s.SaleType = 'DriverLoad' OR (s.SaleType IN ('Cash', 'Credit', 'Installment') AND s.DriverID IS NULL)) AND (adj.AdjDate IS NULL OR s.SaleDate > adj.AdjDate) {(warehouseID.HasValue ? "AND s.WarehouseID = @wid" : "")}), 0)
                    - COALESCE(p.Unit3Factor * p.Unit2Factor, p.Unit3Factor, p.Unit2Factor, 1.0) * ISNULL((SELECT SUM(si.Quantity) FROM SaleItems si JOIN Sales s ON si.SaleID = s.SaleID WHERE si.ProductID = p.ProductID AND si.Factor IS NULL AND s.IsPosted = 1 AND (s.SaleType = 'DriverLoad' OR (s.SaleType IN ('Cash', 'Credit', 'Installment') AND s.DriverID IS NULL)) AND (adj.AdjDate IS NULL OR s.SaleDate > adj.AdjDate) {(warehouseID.HasValue ? "AND s.WarehouseID = @wid" : "")}), 0)
                    -- Outgoing since adjustment: Warehouse Transfers
                    - ISNULL((SELECT SUM(ti.Quantity * ISNULL(ti.Factor, 0)) FROM WarehouseTransferItems ti JOIN WarehouseTransfers t ON ti.TransferID = t.TransferID WHERE ti.ProductID = p.ProductID AND t.IsPosted = 1 AND (adj.AdjDate IS NULL OR t.TransferDate > adj.AdjDate) {(warehouseID.HasValue ? "AND t.FromWarehouseID = @wid" : "")}), 0)
                    - COALESCE(p.Unit3Factor * p.Unit2Factor, p.Unit3Factor, p.Unit2Factor, 1.0) * ISNULL((SELECT SUM(ti.Quantity) FROM WarehouseTransferItems ti JOIN WarehouseTransfers t ON ti.TransferID = t.TransferID WHERE ti.ProductID = p.ProductID AND ti.Factor IS NULL AND t.IsPosted = 1 AND (adj.AdjDate IS NULL OR t.TransferDate > adj.AdjDate) {(warehouseID.HasValue ? "AND t.FromWarehouseID = @wid" : "")}), 0)
                    -- Outgoing since adjustment: Wastage & Loss
                    - ISNULL((SELECT SUM(wli.Quantity * ISNULL(wli.Factor, 0)) FROM WastageLossItems wli JOIN WastageLoss wl ON wli.WastageID = wl.WastageID WHERE wli.ProductID = p.ProductID AND (adj.AdjDate IS NULL OR wl.WastageDate > adj.AdjDate) {(warehouseID.HasValue ? "AND wl.WarehouseID = @wid" : "")}), 0)
                    - COALESCE(p.Unit3Factor * p.Unit2Factor, p.Unit3Factor, p.Unit2Factor, 1.0) * ISNULL((SELECT SUM(wli.Quantity) FROM WastageLossItems wli JOIN WastageLoss wl ON wli.WastageID = wl.WastageID WHERE wli.ProductID = p.ProductID AND wli.Factor IS NULL AND (adj.AdjDate IS NULL OR wl.WastageDate > adj.AdjDate) {(warehouseID.HasValue ? "AND wl.WarehouseID = @wid" : "")}), 0) AS BookQty
                FROM Products p
                OUTER APPLY (
                    SELECT TOP 1 sa.AdjDate, sa.ActualQty, sa.Factor
                    FROM StockAdjustments sa 
                    WHERE sa.ProductID = p.ProductID 
                      {(warehouseID.HasValue ? "AND sa.WarehouseID = @wid" : "")}
                    ORDER BY sa.AdjDate DESC
                ) adj
                WHERE p.ProductID = @pid";

            var val = DbHelper.Scalar(sql, prms.ToArray());
            return val == null || val == DBNull.Value ? 0 : Convert.ToDecimal(val);
        }

        /// <summary>جلب ملخص أرصدة كل الأصناف دفعة واحدة بسرعة فائقة بدون استعلامات فرعية مكررة</summary>
        public static Dictionary<int, decimal> GetStockSummary(int? warehouseID = null)
        {
            var dict = new Dictionary<int, decimal>();
            List<SqlParameter> prms = new List<SqlParameter>();
            if (warehouseID.HasValue) prms.Add(DbHelper.P("@wid", warehouseID.Value));

            string sql = @"
                SELECT ProductID, SUM(NetQty) AS TotalQty
                FROM (
                    SELECT sa.ProductID, sa.ActualQty * COALESCE(sa.Factor, 1.0) AS NetQty
                    FROM StockAdjustments sa
                    INNER JOIN (
                        SELECT ProductID, WarehouseID, MAX(AdjDate) AS MaxDate
                        FROM StockAdjustments
                        " + (warehouseID.HasValue ? "WHERE WarehouseID = @wid" : "") + @"
                        GROUP BY ProductID, WarehouseID
                    ) latest ON sa.ProductID = latest.ProductID AND sa.WarehouseID = latest.WarehouseID AND sa.AdjDate = latest.MaxDate
                    " + (warehouseID.HasValue ? "WHERE sa.WarehouseID = @wid" : "") + @"

                    UNION ALL

                    SELECT pi.ProductID, SUM(pi.Quantity * COALESCE(pi.Factor, 1.0)) AS NetQty
                    FROM PurchaseItems pi
                    JOIN Purchases pu ON pi.PurchaseID = pu.PurchaseID
                    WHERE pu.IsPosted = 1 " + (warehouseID.HasValue ? "AND pu.WarehouseID = @wid" : "") + @"
                    GROUP BY pi.ProductID

                    UNION ALL

                    SELECT ri.ProductID, SUM(ri.Quantity * COALESCE(ri.Factor, 1.0)) AS NetQty
                    FROM ReturnItems ri
                    JOIN SalesReturns sr ON ri.ReturnID = sr.ReturnID
                    " + (warehouseID.HasValue ? "WHERE sr.WarehouseID = @wid" : "") + @"
                    GROUP BY ri.ProductID

                    UNION ALL

                    SELECT hi.ProductID, SUM(hi.ReturnedQty * COALESCE(hi.Factor, 1.0)) AS NetQty
                    FROM HandoverItems hi
                    JOIN DriverHandovers dh ON hi.HandoverID = dh.HandoverID
                    JOIN DriverLoads dl ON dh.LoadID = dl.LoadID
                    " + (warehouseID.HasValue ? "WHERE dl.WarehouseID = @wid" : "") + @"
                    GROUP BY hi.ProductID

                    UNION ALL

                    SELECT ti.ProductID, SUM(ti.Quantity * COALESCE(ti.Factor, 1.0)) AS NetQty
                    FROM WarehouseTransferItems ti
                    JOIN WarehouseTransfers t ON ti.TransferID = t.TransferID
                    WHERE t.IsPosted = 1 " + (warehouseID.HasValue ? "AND t.ToWarehouseID = @wid" : "") + @"
                    GROUP BY ti.ProductID

                    UNION ALL

                    SELECT si.ProductID, -SUM(si.Quantity * COALESCE(si.Factor, 1.0)) AS NetQty
                    FROM SaleItems si
                    JOIN Sales s ON si.SaleID = s.SaleID
                    WHERE s.IsPosted = 1 " + (warehouseID.HasValue ? "AND s.WarehouseID = @wid" : "") + @"
                    GROUP BY si.ProductID

                    UNION ALL

                    SELECT pri.ProductID, -SUM(pri.Quantity * COALESCE(pri.Factor, 1.0)) AS NetQty
                    FROM PurchaseReturnItems pri
                    JOIN PurchaseReturns pr ON pri.ReturnID = pr.ReturnID
                    " + (warehouseID.HasValue ? "WHERE pr.WarehouseID = @wid" : "") + @"
                    GROUP BY pri.ProductID

                    UNION ALL

                    SELECT ti.ProductID, -SUM(ti.Quantity * COALESCE(ti.Factor, 1.0)) AS NetQty
                    FROM WarehouseTransferItems ti
                    JOIN WarehouseTransfers t ON ti.TransferID = t.TransferID
                    WHERE t.IsPosted = 1 " + (warehouseID.HasValue ? "AND t.FromWarehouseID = @wid" : "") + @"
                    GROUP BY ti.ProductID

                    UNION ALL

                    SELECT wli.ProductID, -SUM(wli.Quantity * COALESCE(wli.Factor, 1.0)) AS NetQty
                    FROM WastageLossItems wli
                    JOIN WastageLoss wl ON wli.WastageID = wl.WastageID
                    " + (warehouseID.HasValue ? "WHERE wl.WarehouseID = @wid" : "") + @"
                    GROUP BY wli.ProductID
                ) StockUnion
                GROUP BY ProductID";

            try
            {
                DataTable dt = DbHelper.Query(sql, prms.ToArray());
                foreach (DataRow r in dt.Rows)
                {
                    int pid = Convert.ToInt32(r["ProductID"]);
                    decimal qty = r["TotalQty"] != DBNull.Value ? Convert.ToDecimal(r["TotalQty"]) : 0m;
                    dict[pid] = qty;
                }
            }
            catch { }

            return dict;
        }

        /// <summary>حفظ تسوية جردية لصنف في مخزن محدد</summary>
        public static int SaveAdjustment(int productID, int warehouseID, decimal bookQty, decimal actualQty, string notes, string unitName = null, decimal? factor = null)
        {
            return DbHelper.ExecuteInsert(
                @"INSERT INTO StockAdjustments (ProductID, WarehouseID, BookQty, ActualQty, Notes, CreatedBy, UnitName, Factor)
                  VALUES (@pid, @wid, @bq, @aq, @notes, @by, @un, @fac)",
                DbHelper.P("@pid", productID),
                DbHelper.P("@wid", warehouseID),
                DbHelper.P("@bq", bookQty),
                DbHelper.P("@aq", actualQty),
                DbHelper.P("@notes", notes),
                DbHelper.P("@by", Session.EmpID),
                DbHelper.P("@un", unitName),
                DbHelper.P("@fac", factor ?? (object)DBNull.Value)
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
                    p.Unit1Name, p.Unit1Barcode, p.Unit1SalePrice, p.Unit1PurchasePrice,
                    p.Unit2Name, p.Unit2Factor, p.Unit2Barcode, p.Unit2SalePrice, p.Unit2PurchasePrice,
                    p.Unit3Factor,
                    sa.BookQty,
                    sa.ActualQty,
                    (sa.ActualQty - sa.BookQty) AS DiffQty,
                    sa.Notes,
                    e.EmpName AS CreatedBy,
                    sa.UnitName AS AdjUnitName,
                    sa.Factor AS AdjFactor
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
                        si.Quantity * COALESCE(si.Factor, COALESCE(p.Unit3Factor * p.Unit2Factor, p.Unit3Factor, p.Unit2Factor, 1.0)) AS QtyOut,
                        s.Notes
                    FROM SaleItems si
                    JOIN Sales s ON si.SaleID = s.SaleID
                    JOIN Products p ON si.ProductID = p.ProductID
                    JOIN Warehouses w ON s.WarehouseID = w.WarehouseID
                    LEFT JOIN Clients c ON s.ClientID = c.ClientID
                    LEFT JOIN Employees e ON s.DriverID = e.EmpID
                    WHERE si.ProductID = @pid
                      AND s.IsPosted = 1
                      AND (s.SaleType = 'DriverLoad' OR (s.SaleType IN ('Cash', 'Credit', 'Installment') AND s.DriverID IS NULL))
                      {whFilterSales}
 
                    UNION ALL
 
                    -- 2. Sales Returns (Incoming)
                    SELECT 
                        sr.ReturnDate AS MovDate,
                        N'مرتجع مبيعات' AS MovType,
                        ISNULL(s.SaleCode, N'---') AS RefCode,
                        ISNULL(c.ClientName, N'---') AS PersonName,
                        w.WarehouseName,
                        ri.Quantity * COALESCE(ri.Factor, COALESCE(p.Unit3Factor * p.Unit2Factor, p.Unit3Factor, p.Unit2Factor, 1.0)) AS QtyIn,
                        0.00 AS QtyOut,
                        sr.Notes
                    FROM ReturnItems ri
                    JOIN SalesReturns sr ON ri.ReturnID = sr.ReturnID
                    JOIN Products p ON ri.ProductID = p.ProductID
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
                        hi.ReturnedQty * COALESCE(hi.Factor, COALESCE(p.Unit3Factor * p.Unit2Factor, p.Unit3Factor, p.Unit2Factor, 1.0)) AS QtyIn,
                        0.00 AS QtyOut,
                        dh.Notes
                    FROM HandoverItems hi
                    JOIN DriverHandovers dh ON hi.HandoverID = dh.HandoverID
                    JOIN DriverLoads dl ON dh.LoadID = dl.LoadID
                    JOIN Products p ON hi.ProductID = p.ProductID
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
                        CASE WHEN (sa.ActualQty - sa.BookQty) * COALESCE(sa.Factor, COALESCE(p.Unit3Factor * p.Unit2Factor, p.Unit3Factor, p.Unit2Factor, 1.0)) > 0 THEN (sa.ActualQty - sa.BookQty) * COALESCE(sa.Factor, COALESCE(p.Unit3Factor * p.Unit2Factor, p.Unit3Factor, p.Unit2Factor, 1.0)) ELSE 0.00 END AS QtyIn,
                        CASE WHEN (sa.ActualQty - sa.BookQty) * COALESCE(sa.Factor, COALESCE(p.Unit3Factor * p.Unit2Factor, p.Unit3Factor, p.Unit2Factor, 1.0)) < 0 THEN ABS((sa.ActualQty - sa.BookQty) * COALESCE(sa.Factor, COALESCE(p.Unit3Factor * p.Unit2Factor, p.Unit3Factor, p.Unit2Factor, 1.0))) ELSE 0.00 END AS QtyOut,
                        sa.Notes
                    FROM StockAdjustments sa
                    JOIN Products p ON sa.ProductID = p.ProductID
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
                        pi.Quantity * COALESCE(pi.Factor, COALESCE(p.Unit3Factor * p.Unit2Factor, p.Unit3Factor, p.Unit2Factor, 1.0)) AS QtyIn,
                        0.00 AS QtyOut,
                        pu.Notes
                    FROM PurchaseItems pi
                    JOIN Purchases pu ON pi.PurchaseID = pu.PurchaseID
                    JOIN Products p ON pi.ProductID = p.ProductID
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
                        pri.Quantity * COALESCE(pri.Factor, COALESCE(p.Unit3Factor * p.Unit2Factor, p.Unit3Factor, p.Unit2Factor, 1.0)) AS QtyOut,
                        pr.Notes
                    FROM PurchaseReturnItems pri
                    JOIN PurchaseReturns pr ON pri.ReturnID = pr.ReturnID
                    JOIN Products p ON pri.ProductID = p.ProductID
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
                        ti.Quantity * COALESCE(ti.Factor, COALESCE(p.Unit3Factor * p.Unit2Factor, p.Unit3Factor, p.Unit2Factor, 1.0)) AS QtyIn,
                        0.00 AS QtyOut,
                        t.Notes
                    FROM WarehouseTransferItems ti
                    JOIN WarehouseTransfers t ON ti.TransferID = t.TransferID
                    JOIN Products p ON ti.ProductID = p.ProductID
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
                        ti.Quantity * COALESCE(ti.Factor, COALESCE(p.Unit3Factor * p.Unit2Factor, p.Unit3Factor, p.Unit2Factor, 1.0)) AS QtyOut,
                        t.Notes
                    FROM WarehouseTransferItems ti
                    JOIN WarehouseTransfers t ON ti.TransferID = t.TransferID
                    JOIN Products p ON ti.ProductID = p.ProductID
                    JOIN Warehouses wFrom ON t.FromWarehouseID = wFrom.WarehouseID
                    JOIN Warehouses wTo ON t.ToWarehouseID = wTo.WarehouseID
                    WHERE ti.ProductID = @pid AND t.IsPosted = 1
                      {(warehouseID.HasValue ? "AND t.FromWarehouseID = @wid" : "")}
                ) AS Movements
                ORDER BY MovDate ASC";

            return DbHelper.Query(sql, prms.ToArray());
        }

        public static int GetBelowMinStockCount()
        {
            try
            {
                // We sum CurrentQty across all warehouses per product and see if it's below MinStockLimit
                object val = DbHelper.Scalar(@"
                    SELECT COUNT(1)
                    FROM (
                        SELECT ProductID, MinStockLimit, SUM(CurrentQty) AS TotalStock
                        FROM vw_CurrentStockByWarehouse
                        GROUP BY ProductID, MinStockLimit
                    ) AS t
                    WHERE t.MinStockLimit > 0 AND t.TotalStock <= t.MinStockLimit");
                return val != null ? Convert.ToInt32(val) : 0;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>حفظ تاريخ بداية عملية الجرد الحالية (حسب المخزن أو إجمالي)</summary>
        public static void SetInventoryStartDate(int? warehouseID, DateTime startDate)
        {
            string key = "InventoryStartDate_" + (warehouseID.HasValue ? warehouseID.Value.ToString() : "ALL");
            DbHelper.SetAppSetting(key, startDate.ToString("yyyy-MM-dd HH:mm:ss"));
        }

        /// <summary>جلب تاريخ بداية عملية الجرد الحالية (حسب المخزن أو إجمالي)</summary>
        public static DateTime? GetInventoryStartDate(int? warehouseID)
        {
            string key = "InventoryStartDate_" + (warehouseID.HasValue ? warehouseID.Value.ToString() : "ALL");
            string val = DbHelper.GetAppSetting(key, null);
            if (!string.IsNullOrEmpty(val) && DateTime.TryParse(val, out DateTime dt))
                return dt;

            // fallback: قراءة من ملف محلي
            try
            {
                string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "inventory_session.ini");
                if (System.IO.File.Exists(path))
                {
                    string content = System.IO.File.ReadAllText(path);
                    if (content.StartsWith(key + "="))
                    {
                        string dateStr = content.Substring(key.Length + 1).Trim();
                        if (DateTime.TryParse(dateStr, out DateTime dtFile))
                            return dtFile;
                    }
                }
            }
            catch { }
            return null;
        }

        /// <summary>جلب قائمة أرقام الأصناف التي تم جردها (لها تسوية) بعد تاريخ معين في مخزن محدد</summary>
        public static HashSet<int> GetInventoriedProductIDs(DateTime sinceDate, int? warehouseID)
        {
            var set = new HashSet<int>();
            try
            {
                var prms = new List<SqlParameter> { DbHelper.P("@since", sinceDate) };
                string whFilter = "";
                if (warehouseID.HasValue)
                {
                    whFilter = " AND sa.WarehouseID = @wid";
                    prms.Add(DbHelper.P("@wid", warehouseID.Value));
                }

                string sql = $@"
                    SELECT DISTINCT sa.ProductID
                    FROM StockAdjustments sa
                    WHERE sa.AdjDate >= @since {whFilter}";

                DataTable dt = DbHelper.Query(sql, prms.ToArray());
                foreach (DataRow r in dt.Rows)
                {
                    set.Add(Convert.ToInt32(r["ProductID"]));
                }
            }
            catch { }
            return set;
        }
    }
}

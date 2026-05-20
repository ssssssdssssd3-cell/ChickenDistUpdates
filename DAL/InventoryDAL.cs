using System;
using System.Data;
using System.Data.SqlClient;
using ChickenDist.Core;

namespace ChickenDist.DAL
{
    public static class InventoryDAL
    {
        /// <summary>جلب رصيد الجرد الحالي لكل الأصناف مع إمكانية البحث</summary>
        public static DataTable GetStock(string searchTerm = "")
        {
            string filter = "";
            SqlParameter[] prms = new SqlParameter[0];
            if (!string.IsNullOrEmpty(searchTerm))
            {
                filter = " AND (p.ProductName LIKE @term OR p.ProductCode LIKE @term) ";
                prms = new[] { DbHelper.P("@term", "%" + searchTerm + "%") };
            }

            string sql = $@"
                SELECT 
                    p.ProductID,
                    p.ProductCode,
                    p.ProductName,
                    p.Unit,
                    p.SalePrice,
                    ISNULL(adj.ActualQty, 0) + 
                    -- Incoming since adjustment
                    ISNULL((SELECT SUM(ri.Quantity) 
                            FROM ReturnItems ri 
                            JOIN SalesReturns sr ON ri.ReturnID = sr.ReturnID 
                            WHERE ri.ProductID = p.ProductID 
                              AND (adj.AdjDate IS NULL OR sr.ReturnDate > adj.AdjDate)), 0) +
                    ISNULL((SELECT SUM(hi.ReturnedQty) 
                            FROM HandoverItems hi 
                            JOIN DriverHandovers dh ON hi.HandoverID = dh.HandoverID
                            WHERE hi.ProductID = p.ProductID 
                              AND (adj.AdjDate IS NULL OR dh.HandoverDate > adj.AdjDate)), 0) -
                    -- Outgoing since adjustment
                    ISNULL((SELECT SUM(si.Quantity) 
                            FROM SaleItems si 
                            JOIN Sales s ON si.SaleID = s.SaleID
                            WHERE si.ProductID = p.ProductID 
                              AND (s.SaleType = 'DriverLoad' OR s.DriverID IS NULL)
                              AND (adj.AdjDate IS NULL OR s.SaleDate > adj.AdjDate)), 0) AS BookQty
                FROM Products p
                OUTER APPLY (
                    SELECT TOP 1 sa.AdjDate, sa.ActualQty 
                    FROM StockAdjustments sa 
                    WHERE sa.ProductID = p.ProductID 
                    ORDER BY sa.AdjDate DESC
                ) adj
                WHERE p.IsActive = 1 {filter}
                ORDER BY p.ProductName";

            return DbHelper.Query(sql, prms);
        }

        public static decimal GetProductStock(int productID)
        {
            string sql = @"
                SELECT 
                    ISNULL(adj.ActualQty, 0) + 
                    -- Incoming since adjustment
                    ISNULL((SELECT SUM(ri.Quantity) 
                            FROM ReturnItems ri 
                            JOIN SalesReturns sr ON ri.ReturnID = sr.ReturnID 
                            WHERE ri.ProductID = p.ProductID 
                              AND (adj.AdjDate IS NULL OR sr.ReturnDate > adj.AdjDate)), 0) +
                    ISNULL((SELECT SUM(hi.ReturnedQty) 
                            FROM HandoverItems hi 
                            JOIN DriverHandovers dh ON hi.HandoverID = dh.HandoverID
                            WHERE hi.ProductID = p.ProductID 
                              AND (adj.AdjDate IS NULL OR dh.HandoverDate > adj.AdjDate)), 0) -
                    -- Outgoing since adjustment
                    ISNULL((SELECT SUM(si.Quantity) 
                            FROM SaleItems si 
                            JOIN Sales s ON si.SaleID = s.SaleID
                            WHERE si.ProductID = p.ProductID 
                              AND (s.SaleType = 'DriverLoad' OR s.DriverID IS NULL)
                              AND (adj.AdjDate IS NULL OR s.SaleDate > adj.AdjDate)), 0) AS BookQty
                FROM Products p
                OUTER APPLY (
                    SELECT TOP 1 sa.AdjDate, sa.ActualQty 
                    FROM StockAdjustments sa 
                    WHERE sa.ProductID = p.ProductID 
                    ORDER BY sa.AdjDate DESC
                ) adj
                WHERE p.ProductID = @pid";
            var val = DbHelper.Scalar(sql, DbHelper.P("@pid", productID));
            return val == null || val == DBNull.Value ? 0 : Convert.ToDecimal(val);
        }

        /// <summary>حفظ تسوية جردية لصنف</summary>
        public static int SaveAdjustment(int productID, decimal bookQty, decimal actualQty, string notes)
        {
            return DbHelper.ExecuteInsert(
                @"INSERT INTO StockAdjustments(ProductID, BookQty, ActualQty, Notes, CreatedBy)
                  VALUES(@pid, @bq, @aq, @notes, @by)",
                DbHelper.P("@pid", productID),
                DbHelper.P("@bq", bookQty),
                DbHelper.P("@aq", actualQty),
                DbHelper.P("@notes", notes),
                DbHelper.P("@by", Session.EmpID)
            );
        }

        /// <summary>جلب تاريخ تسويات الجرد للفترة</summary>
        public static DataTable GetAdjustments(DateTime from, DateTime to, string searchTerm = "")
        {
            string filter = "";
            SqlParameter[] prms = new[] { DbHelper.P("@f", from.Date), DbHelper.P("@t", to.Date) };
            if (!string.IsNullOrEmpty(searchTerm))
            {
                filter = " AND (p.ProductName LIKE @term OR p.ProductCode LIKE @term OR sa.Notes LIKE @term) ";
                prms = new[] { 
                    DbHelper.P("@f", from.Date), 
                    DbHelper.P("@t", to.Date), 
                    DbHelper.P("@term", "%" + searchTerm + "%") 
                };
            }

            string sql = $@"
                SELECT 
                    sa.AdjID,
                    sa.AdjDate,
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
                LEFT JOIN Employees e ON sa.CreatedBy = e.EmpID
                WHERE CAST(sa.AdjDate AS DATE) BETWEEN @f AND @t {filter}
                ORDER BY sa.AdjDate DESC";

            return DbHelper.Query(sql, prms);
        }

        /// <summary>جلب تقرير حركة الصنف الكامل</summary>
        public static DataTable GetProductMovement(int productID)
        {
            string sql = @"
                SELECT 
                    MovDate AS TransDate,
                    MovType AS TransType,
                    RefCode,
                    PersonName,
                    QtyIn,
                    QtyOut,
                    Notes
                FROM (
                    -- 1. Direct Sales / Driver Loads (Outgoing)
                    SELECT 
                        s.SaleDate AS MovDate,
                        CASE s.SaleType 
                            WHEN 'Cash' THEN N'بيع نقدي' 
                            WHEN 'Credit' THEN N'بيع آجل' 
                            ELSE N'تحميل حمولة مندوب' 
                        END AS MovType,
                        s.SaleCode AS RefCode,
                        CASE s.SaleType
                            WHEN 'DriverLoad' THEN ISNULL(e.EmpName, N'---')
                            ELSE ISNULL(c.ClientName, N'---')
                        END AS PersonName,
                        0.00 AS QtyIn,
                        si.Quantity AS QtyOut,
                        s.Notes
                    FROM SaleItems si
                    JOIN Sales s ON si.SaleID = s.SaleID
                    LEFT JOIN Clients c ON s.ClientID = c.ClientID
                    LEFT JOIN Employees e ON s.DriverID = e.EmpID
                    WHERE si.ProductID = @pid

                    UNION ALL

                    -- 2. Sales Returns (Incoming)
                    SELECT 
                        sr.ReturnDate AS MovDate,
                        N'مرتجع مبيعات' AS MovType,
                        ISNULL(s.SaleCode, N'---') AS RefCode,
                        ISNULL(c.ClientName, N'---') AS PersonName,
                        ri.Quantity AS QtyIn,
                        0.00 AS QtyOut,
                        sr.Notes
                    FROM ReturnItems ri
                    JOIN SalesReturns sr ON ri.ReturnID = sr.ReturnID
                    LEFT JOIN Sales s ON sr.SaleID = s.SaleID
                    LEFT JOIN Clients c ON sr.ClientID = c.ClientID
                    WHERE ri.ProductID = @pid

                    UNION ALL

                    -- 3. Driver Handovers (Incoming returned chicks)
                    SELECT 
                        dh.HandoverDate AS MovDate,
                        N'مرتجع حمولة مندوب' AS MovType,
                        ISNULL(s.SaleCode, N'---') AS RefCode,
                        ISNULL(e.EmpName, N'---') AS PersonName,
                        hi.ReturnedQty AS QtyIn,
                        0.00 AS QtyOut,
                        dh.Notes
                    FROM HandoverItems hi
                    JOIN DriverHandovers dh ON hi.HandoverID = dh.HandoverID
                    JOIN DriverLoads dl ON dh.LoadID = dl.LoadID
                    JOIN Sales s ON dl.SaleID = s.SaleID
                    LEFT JOIN Employees e ON dh.DriverID = e.EmpID
                    WHERE hi.ProductID = @pid AND hi.ReturnedQty > 0

                    UNION ALL

                    -- 4. Stock Adjustments (Incoming or Outgoing)
                    SELECT 
                        sa.AdjDate AS MovDate,
                        N'تسوية جردية' AS MovType,
                        N'تسوية #' + CAST(sa.AdjID AS NVARCHAR(20)) AS RefCode,
                        ISNULL(e.EmpName, N'---') AS PersonName,
                        CASE WHEN (sa.ActualQty - sa.BookQty) > 0 THEN (sa.ActualQty - sa.BookQty) ELSE 0.00 END AS QtyIn,
                        CASE WHEN (sa.ActualQty - sa.BookQty) < 0 THEN ABS(sa.ActualQty - sa.BookQty) ELSE 0.00 END AS QtyOut,
                        sa.Notes
                    FROM StockAdjustments sa
                    LEFT JOIN Employees e ON sa.CreatedBy = e.EmpID
                    WHERE sa.ProductID = @pid
                ) AS Movements
                ORDER BY MovDate ASC";

            return DbHelper.Query(sql, DbHelper.P("@pid", productID));
        }
    }
}

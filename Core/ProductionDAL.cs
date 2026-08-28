using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace ChickenDist.Core
{
    #region Data Models
    public class BOMItemModel
    {
        public int BOMItemID { get; set; }
        public int BOMID { get; set; }
        public int RawProductID { get; set; }
        public string RawProductCode { get; set; }
        public string RawProductName { get; set; }
        public decimal Quantity { get; set; }
        public string UnitName { get; set; }
        public decimal Factor { get; set; } = 1m;
        public decimal RawCostPrice { get; set; }
        public decimal TotalCost => Quantity * RawCostPrice;
        public string Notes { get; set; }
    }

    public class BOMModel
    {
        public int BOMID { get; set; }
        public int ProductID { get; set; }
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public decimal OutputQty { get; set; } = 1m;
        public string UnitName { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime LastUpdated { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;
        public List<BOMItemModel> Items { get; set; } = new List<BOMItemModel>();

        public decimal TotalRawCost
        {
            get
            {
                decimal sum = 0;
                if (Items != null)
                {
                    foreach (var itm in Items) sum += itm.TotalCost;
                }
                return sum;
            }
        }

        public decimal UnitCost => OutputQty > 0 ? TotalRawCost / OutputQty : TotalRawCost;
    }

    public class ProductionOrderItemModel
    {
        public int ItemID { get; set; }
        public int ProductionID { get; set; }
        public int RawProductID { get; set; }
        public string RawProductCode { get; set; }
        public string RawProductName { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalCost => Quantity * UnitCost;
        public string UnitName { get; set; }
        public decimal Factor { get; set; } = 1m;
        public string Notes { get; set; }
    }

    public class ProductionOrderModel
    {
        public int ProductionID { get; set; }
        public string OrderCode { get; set; }
        public string ProductionType { get; set; } = "Fixed"; // "Fixed" or "Custom"
        public int? BOMID { get; set; }
        public int FinishedProductID { get; set; }
        public string FinishedProductCode { get; set; }
        public string FinishedProductName { get; set; }
        public decimal ProducedQty { get; set; } = 1m;
        public string UnitName { get; set; }
        public int WarehouseID { get; set; } = 1;
        public string WarehouseName { get; set; }
        public decimal RawMaterialsCost { get; set; }
        public decimal ExtraExpenses { get; set; }
        public string ExpensesNotes { get; set; }
        public decimal TotalCost => RawMaterialsCost + ExtraExpenses;
        public decimal UnitCost => ProducedQty > 0 ? TotalCost / ProducedQty : TotalCost;
        public string Status { get; set; } = "InPreparation"; // "InPreparation", "Completed", "Cancelled"
        public bool StockDeducted { get; set; }
        public bool StockAdded { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime UpdatedDate { get; set; } = DateTime.Now;
        public DateTime? CompletedDate { get; set; }
        public int? CreatedBy { get; set; }
        public string CreatedByName { get; set; }
        public int? UpdatedBy { get; set; }
        public string Notes { get; set; }
        public List<ProductionOrderItemModel> Items { get; set; } = new List<ProductionOrderItemModel>();
    }
    #endregion

    public static class ProductionDAL
    {
        #region BOM Methods (شجرة مواد التصنيع)
        public static BOMModel GetBOMByProductID(int productId)
        {
            try
            {
                var dt = DbHelper.Query(@"
                    SELECT TOP 1 b.*, p.ProductCode, p.ProductName
                    FROM BOMHeader b
                    JOIN Products p ON b.ProductID = p.ProductID
                    WHERE b.ProductID = @pid AND b.IsActive = 1
                    ORDER BY b.BOMID DESC",
                    DbHelper.P("@pid", productId));

                if (dt == null || dt.Rows.Count == 0) return null;
                return MapBOM(dt.Rows[0]);
            }
            catch (Exception ex)
            {
                AppLogger.Error("ProductionDAL.GetBOMByProductID", ex);
                return null;
            }
        }

        public static BOMModel GetBOMByID(int bomId)
        {
            try
            {
                var dt = DbHelper.Query(@"
                    SELECT b.*, p.ProductCode, p.ProductName
                    FROM BOMHeader b
                    JOIN Products p ON b.ProductID = p.ProductID
                    WHERE b.BOMID = @id",
                    DbHelper.P("@id", bomId));

                if (dt == null || dt.Rows.Count == 0) return null;
                return MapBOM(dt.Rows[0]);
            }
            catch (Exception ex)
            {
                AppLogger.Error("ProductionDAL.GetBOMByID", ex);
                return null;
            }
        }

        public static DataTable GetAllBOMs(string search = "")
        {
            try
            {
                string sql = @"
                    SELECT b.BOMID, b.ProductID, p.ProductCode, p.ProductName,
                           b.OutputQty, b.UnitName, b.Notes, b.LastUpdated,
                           (SELECT COUNT(1) FROM BOMItems bi WHERE bi.BOMID = b.BOMID) AS ItemsCount,
                           (SELECT COALESCE(SUM(bi.Quantity * COALESCE(rp.CostPrice, 0)), 0)
                            FROM BOMItems bi
                            JOIN Products rp ON bi.RawProductID = rp.ProductID
                            WHERE bi.BOMID = b.BOMID) AS TotalEstCost
                    FROM BOMHeader b
                    JOIN Products p ON b.ProductID = p.ProductID
                    WHERE b.IsActive = 1";

                if (!string.IsNullOrWhiteSpace(search))
                {
                    sql += " AND (p.ProductName LIKE @q OR p.ProductCode LIKE @q OR b.Notes LIKE @q)";
                    return DbHelper.Query(sql + " ORDER BY p.ProductName ASC", DbHelper.P("@q", "%" + search.Trim() + "%"));
                }
                return DbHelper.Query(sql + " ORDER BY p.ProductName ASC");
            }
            catch (Exception ex)
            {
                AppLogger.Error("ProductionDAL.GetAllBOMs", ex);
                return new DataTable();
            }
        }

        private static BOMModel MapBOM(DataRow row)
        {
            var bom = new BOMModel
            {
                BOMID = Convert.ToInt32(row["BOMID"]),
                ProductID = Convert.ToInt32(row["ProductID"]),
                ProductCode = row["ProductCode"]?.ToString(),
                ProductName = row["ProductName"]?.ToString(),
                OutputQty = Convert.ToDecimal(row["OutputQty"]),
                UnitName = row["UnitName"]?.ToString(),
                Notes = row["Notes"]?.ToString(),
                CreatedDate = Convert.ToDateTime(row["CreatedDate"]),
                LastUpdated = Convert.ToDateTime(row["LastUpdated"]),
                IsActive = Convert.ToBoolean(row["IsActive"])
            };

            var dtItems = DbHelper.Query(@"
                SELECT bi.*, p.ProductCode AS RawProductCode, p.ProductName AS RawProductName, COALESCE(p.CostPrice, 0) AS RawCostPrice
                FROM BOMItems bi
                JOIN Products p ON bi.RawProductID = p.ProductID
                WHERE bi.BOMID = @bid
                ORDER BY bi.BOMItemID ASC",
                DbHelper.P("@bid", bom.BOMID));

            if (dtItems != null)
            {
                foreach (DataRow r in dtItems.Rows)
                {
                    bom.Items.Add(new BOMItemModel
                    {
                        BOMItemID = Convert.ToInt32(r["BOMItemID"]),
                        BOMID = bom.BOMID,
                        RawProductID = Convert.ToInt32(r["RawProductID"]),
                        RawProductCode = r["RawProductCode"]?.ToString(),
                        RawProductName = r["RawProductName"]?.ToString(),
                        Quantity = Convert.ToDecimal(r["Quantity"]),
                        UnitName = r["UnitName"]?.ToString(),
                        Factor = Convert.ToDecimal(r["Factor"] == DBNull.Value ? 1 : r["Factor"]),
                        RawCostPrice = Convert.ToDecimal(r["RawCostPrice"]),
                        Notes = r["Notes"]?.ToString()
                    });
                }
            }

            return bom;
        }

        public static int SaveBOM(BOMModel bom)
        {
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var trans = conn.BeginTransaction())
                {
                    try
                    {
                        if (bom.BOMID <= 0)
                        {
                            object existingId = DbHelper.ScalarTrans(trans,
                                "SELECT BOMID FROM BOMHeader WHERE ProductID = @pid AND IsActive = 1",
                                DbHelper.P("@pid", bom.ProductID));

                            if (existingId != null && existingId != DBNull.Value)
                            {
                                bom.BOMID = Convert.ToInt32(existingId);
                            }
                        }

                        if (bom.BOMID <= 0)
                        {
                            object newId = DbHelper.ScalarTrans(trans, @"
                                INSERT INTO BOMHeader (ProductID, OutputQty, UnitName, Notes, CreatedDate, LastUpdated, IsActive)
                                VALUES (@pid, @outQty, @u, @notes, GETDATE(), GETDATE(), 1);
                                SELECT SCOPE_IDENTITY();",
                                DbHelper.P("@pid", bom.ProductID),
                                DbHelper.P("@outQty", bom.OutputQty),
                                DbHelper.P("@u", bom.UnitName),
                                DbHelper.P("@notes", bom.Notes));

                            bom.BOMID = Convert.ToInt32(newId);
                        }
                        else
                        {
                            DbHelper.ExecuteTrans(trans, @"
                                UPDATE BOMHeader
                                SET ProductID = @pid, OutputQty = @outQty, UnitName = @u, Notes = @notes, LastUpdated = GETDATE()
                                WHERE BOMID = @bid",
                                DbHelper.P("@pid", bom.ProductID),
                                DbHelper.P("@outQty", bom.OutputQty),
                                DbHelper.P("@u", bom.UnitName),
                                DbHelper.P("@notes", bom.Notes),
                                DbHelper.P("@bid", bom.BOMID));

                            DbHelper.ExecuteTrans(trans, "DELETE FROM BOMItems WHERE BOMID = @bid", DbHelper.P("@bid", bom.BOMID));
                        }

                        foreach (var itm in bom.Items)
                        {
                            DbHelper.ExecuteTrans(trans, @"
                                INSERT INTO BOMItems (BOMID, RawProductID, Quantity, UnitName, Factor, Notes)
                                VALUES (@bid, @rpid, @qty, @u, @fac, @notes)",
                                DbHelper.P("@bid", bom.BOMID),
                                DbHelper.P("@rpid", itm.RawProductID),
                                DbHelper.P("@qty", itm.Quantity),
                                DbHelper.P("@u", itm.UnitName),
                                DbHelper.P("@fac", itm.Factor <= 0 ? 1 : itm.Factor),
                                DbHelper.P("@notes", itm.Notes));
                        }

                        trans.Commit();
                        return bom.BOMID;
                    }
                    catch
                    {
                        trans.Rollback();
                        throw;
                    }
                }
            }
        }

        public static bool DeleteBOM(int bomId)
        {
            try
            {
                DbHelper.Execute("UPDATE BOMHeader SET IsActive = 0 WHERE BOMID = @id", DbHelper.P("@id", bomId));
                return true;
            }
            catch (Exception ex)
            {
                AppLogger.Error("ProductionDAL.DeleteBOM", ex);
                return false;
            }
        }
        #endregion

        #region Production Orders Methods (أوامر التصنيع الثابت والمخصص)
        public static string GenerateOrderCode(string prefix = "PRD")
        {
            try
            {
                string dateStr = DateTime.Now.ToString("yyMMdd");
                string pat = $"{prefix}-{dateStr}-%";
                object cntObj = DbHelper.Scalar("SELECT COUNT(1) FROM ProductionOrders WHERE OrderCode LIKE @pat", DbHelper.P("@pat", pat));
                int seq = (cntObj != null && cntObj != DBNull.Value) ? Convert.ToInt32(cntObj) + 1 : 1;
                return $"{prefix}-{dateStr}-{seq:D4}";
            }
            catch
            {
                return $"{prefix}-{DateTime.Now.Ticks % 1000000:D6}";
            }
        }

        public static void AdjustStock(SqlTransaction trans, int productId, int warehouseId, decimal qtyDelta)
        {
            DbHelper.ExecuteTrans(trans, @"
                IF EXISTS (SELECT 1 FROM ProductStock WHERE ProductID=@pid AND WarehouseID=@wid)
                    UPDATE ProductStock SET Quantity = Quantity + @delta, LastUpdated = GETDATE() WHERE ProductID=@pid AND WarehouseID=@wid
                ELSE
                    INSERT INTO ProductStock (ProductID, WarehouseID, Quantity, LastUpdated) VALUES (@pid, @wid, @delta, GETDATE())",
                DbHelper.P("@pid", productId), DbHelper.P("@wid", warehouseId), DbHelper.P("@delta", qtyDelta));

            DbHelper.ExecuteTrans(trans, @"
                UPDATE Products SET TotalQuantity = (SELECT COALESCE(SUM(Quantity), 0) FROM ProductStock WHERE ProductID=@pid)
                WHERE ProductID=@pid",
                DbHelper.P("@pid", productId));
        }

        public static int SaveProductionOrder(ProductionOrderModel order, bool completeOrder, string actionUserName)
        {
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var trans = conn.BeginTransaction())
                {
                    try
                    {
                        decimal rawCost = 0;
                        foreach (var itm in order.Items)
                        {
                            rawCost += itm.TotalCost;
                        }
                        order.RawMaterialsCost = rawCost;
                        decimal totalCost = order.RawMaterialsCost + order.ExtraExpenses;
                        decimal unitCost = order.ProducedQty > 0 ? (totalCost / order.ProducedQty) : totalCost;

                        string newStatus = completeOrder ? "Completed" : "InPreparation";

                        if (order.ProductionID <= 0)
                        {
                            if (string.IsNullOrWhiteSpace(order.OrderCode))
                                order.OrderCode = GenerateOrderCode(order.ProductionType == "Custom" ? "CPRD" : "PRD");

                            object newId = DbHelper.ScalarTrans(trans, @"
                                INSERT INTO ProductionOrders (
                                    OrderCode, ProductionType, BOMID, FinishedProductID, ProducedQty, UnitName,
                                    WarehouseID, RawMaterialsCost, ExtraExpenses, ExpensesNotes, TotalCost, UnitCost,
                                    Status, StockDeducted, StockAdded, CreatedDate, UpdatedDate, CompletedDate,
                                    CreatedBy, UpdatedBy, Notes
                                ) VALUES (
                                    @code, @type, @bid, @fpid, @pqty, @u,
                                    @wid, @rawCost, @extra, @expNotes, @totCost, @unitCost,
                                    @status, 0, 0, GETDATE(), GETDATE(), @compDate,
                                    @cby, @uby, @notes
                                );
                                SELECT SCOPE_IDENTITY();",
                                DbHelper.P("@code", order.OrderCode),
                                DbHelper.P("@type", order.ProductionType ?? "Fixed"),
                                DbHelper.P("@bid", (object)order.BOMID ?? DBNull.Value),
                                DbHelper.P("@fpid", order.FinishedProductID),
                                DbHelper.P("@pqty", order.ProducedQty),
                                DbHelper.P("@u", order.UnitName),
                                DbHelper.P("@wid", order.WarehouseID),
                                DbHelper.P("@rawCost", order.RawMaterialsCost),
                                DbHelper.P("@extra", order.ExtraExpenses),
                                DbHelper.P("@expNotes", order.ExpensesNotes),
                                DbHelper.P("@totCost", totalCost),
                                DbHelper.P("@unitCost", unitCost),
                                DbHelper.P("@status", newStatus),
                                DbHelper.P("@compDate", completeOrder ? (object)DateTime.Now : DBNull.Value),
                                DbHelper.P("@cby", (object)order.CreatedBy ?? Session.EmpID),
                                DbHelper.P("@uby", Session.EmpID),
                                DbHelper.P("@notes", order.Notes));

                            order.ProductionID = Convert.ToInt32(newId);

                            foreach (var itm in order.Items)
                            {
                                DbHelper.ExecuteTrans(trans, @"
                                    INSERT INTO ProductionOrderItems (ProductionID, RawProductID, Quantity, UnitCost, TotalCost, UnitName, Factor, Notes)
                                    VALUES (@pid, @rpid, @qty, @cost, @tot, @u, @fac, @notes)",
                                    DbHelper.P("@pid", order.ProductionID),
                                    DbHelper.P("@rpid", itm.RawProductID),
                                    DbHelper.P("@qty", itm.Quantity),
                                    DbHelper.P("@cost", itm.UnitCost),
                                    DbHelper.P("@tot", itm.TotalCost),
                                    DbHelper.P("@u", itm.UnitName),
                                    DbHelper.P("@fac", itm.Factor <= 0 ? 1 : itm.Factor),
                                    DbHelper.P("@notes", itm.Notes));

                                AdjustStock(trans, itm.RawProductID, order.WarehouseID, -itm.Quantity);
                            }

                            DbHelper.ExecuteTrans(trans, "UPDATE ProductionOrders SET StockDeducted = 1 WHERE ProductionID = @id",
                                DbHelper.P("@id", order.ProductionID));

                            if (completeOrder)
                            {
                                AdjustStock(trans, order.FinishedProductID, order.WarehouseID, order.ProducedQty);

                                DbHelper.ExecuteTrans(trans, @"
                                    UPDATE Products SET CostPrice = @uc WHERE ProductID = @fpid",
                                    DbHelper.P("@uc", unitCost),
                                    DbHelper.P("@fpid", order.FinishedProductID));

                                DbHelper.ExecuteTrans(trans, "UPDATE ProductionOrders SET StockAdded = 1 WHERE ProductionID = @id",
                                    DbHelper.P("@id", order.ProductionID));

                                AddHistoryTrans(trans, order.ProductionID, "Completed", actionUserName,
                                    $"تم إتمام وترحيل أمر التصنيع بنجاح. الكمية المنتجة: {order.ProducedQty} {order.UnitName}. تكلفة الوحدة: {unitCost:N2} ج.م.");
                            }
                            else
                            {
                                AddHistoryTrans(trans, order.ProductionID, "Created_Pending", actionUserName,
                                    $"تم إنشاء أمر التصنيع وتعليقه (تحت التحضير). تم خصم {order.Items.Count} صنف من المواد الخام من المخزن.");
                            }
                        }
                        else
                        {
                            var dtOldItems = DbHelper.QueryTrans(trans,
                                "SELECT RawProductID, Quantity FROM ProductionOrderItems WHERE ProductionID = @id",
                                DbHelper.P("@id", order.ProductionID));

                            if (dtOldItems != null)
                            {
                                foreach (DataRow r in dtOldItems.Rows)
                                {
                                    int oldPid = Convert.ToInt32(r["RawProductID"]);
                                    decimal oldQty = Convert.ToDecimal(r["Quantity"]);
                                    AdjustStock(trans, oldPid, order.WarehouseID, oldQty);
                                }
                            }

                            DbHelper.ExecuteTrans(trans, @"
                                UPDATE ProductionOrders
                                SET FinishedProductID = @fpid, ProducedQty = @pqty, UnitName = @u,
                                    WarehouseID = @wid, RawMaterialsCost = @rawCost, ExtraExpenses = @extra,
                                    ExpensesNotes = @expNotes, TotalCost = @totCost, UnitCost = @unitCost,
                                    Status = @status, UpdatedDate = GETDATE(), UpdatedBy = @uby, Notes = @notes,
                                    CompletedDate = CASE WHEN @status = 'Completed' THEN GETDATE() ELSE CompletedDate END
                                WHERE ProductionID = @id",
                                DbHelper.P("@fpid", order.FinishedProductID),
                                DbHelper.P("@pqty", order.ProducedQty),
                                DbHelper.P("@u", order.UnitName),
                                DbHelper.P("@wid", order.WarehouseID),
                                DbHelper.P("@rawCost", order.RawMaterialsCost),
                                DbHelper.P("@extra", order.ExtraExpenses),
                                DbHelper.P("@expNotes", order.ExpensesNotes),
                                DbHelper.P("@totCost", totalCost),
                                DbHelper.P("@unitCost", unitCost),
                                DbHelper.P("@status", newStatus),
                                DbHelper.P("@uby", Session.EmpID),
                                DbHelper.P("@notes", order.Notes),
                                DbHelper.P("@id", order.ProductionID));

                            DbHelper.ExecuteTrans(trans, "DELETE FROM ProductionOrderItems WHERE ProductionID = @id",
                                DbHelper.P("@id", order.ProductionID));

                            foreach (var itm in order.Items)
                            {
                                DbHelper.ExecuteTrans(trans, @"
                                    INSERT INTO ProductionOrderItems (ProductionID, RawProductID, Quantity, UnitCost, TotalCost, UnitName, Factor, Notes)
                                    VALUES (@pid, @rpid, @qty, @cost, @tot, @u, @fac, @notes)",
                                    DbHelper.P("@pid", order.ProductionID),
                                    DbHelper.P("@rpid", itm.RawProductID),
                                    DbHelper.P("@qty", itm.Quantity),
                                    DbHelper.P("@cost", itm.UnitCost),
                                    DbHelper.P("@tot", itm.TotalCost),
                                    DbHelper.P("@u", itm.UnitName),
                                    DbHelper.P("@fac", itm.Factor <= 0 ? 1 : itm.Factor),
                                    DbHelper.P("@notes", itm.Notes));

                                AdjustStock(trans, itm.RawProductID, order.WarehouseID, -itm.Quantity);
                            }

                            if (completeOrder)
                            {
                                AdjustStock(trans, order.FinishedProductID, order.WarehouseID, order.ProducedQty);

                                DbHelper.ExecuteTrans(trans, @"
                                    UPDATE Products SET CostPrice = @uc WHERE ProductID = @fpid",
                                    DbHelper.P("@uc", unitCost),
                                    DbHelper.P("@fpid", order.FinishedProductID));

                                DbHelper.ExecuteTrans(trans, "UPDATE ProductionOrders SET StockAdded = 1 WHERE ProductionID = @id",
                                    DbHelper.P("@id", order.ProductionID));

                                AddHistoryTrans(trans, order.ProductionID, "Completed", actionUserName,
                                    $"تم إنهاء وتأكيد إتمام التصنيع بعد التعديل. الكمية المضافة للمخزن: {order.ProducedQty} {order.UnitName}. تكلفة الوحدة: {unitCost:N2} ج.م.");
                            }
                            else
                            {
                                AddHistoryTrans(trans, order.ProductionID, "Updated_Pending", actionUserName,
                                    $"تم تعديل أمر التصنيع وإعادة تعليقه تحت التحضير مع تسوية فروق المواد الخام بالمخزن.");
                            }
                        }

                        trans.Commit();
                        ProductCache.Refresh();
                        return order.ProductionID;
                    }
                    catch
                    {
                        trans.Rollback();
                        throw;
                    }
                }
            }
        }

        private static void AddHistoryTrans(SqlTransaction trans, int productionId, string actionType, string actionBy, string details)
        {
            DbHelper.ExecuteTrans(trans, @"
                INSERT INTO ProductionOrderHistory (ProductionID, ActionType, ActionDate, ActionBy, Details)
                VALUES (@pid, @type, GETDATE(), @by, @details)",
                DbHelper.P("@pid", productionId),
                DbHelper.P("@type", actionType),
                DbHelper.P("@by", actionBy ?? Session.EmpName ?? "المستخدم"),
                DbHelper.P("@details", details));
        }

        public static ProductionOrderModel GetProductionOrderByID(int productionId)
        {
            try
            {
                var dt = DbHelper.Query(@"
                    SELECT po.*, p.ProductCode AS FinishedProductCode, p.ProductName AS FinishedProductName,
                           w.WarehouseName, e.FullName AS CreatedByName
                    FROM ProductionOrders po
                    JOIN Products p ON po.FinishedProductID = p.ProductID
                    LEFT JOIN Warehouses w ON po.WarehouseID = w.WarehouseID
                    LEFT JOIN Employees e ON po.CreatedBy = e.EmpID
                    WHERE po.ProductionID = @id",
                    DbHelper.P("@id", productionId));

                if (dt == null || dt.Rows.Count == 0) return null;
                var r = dt.Rows[0];

                var order = new ProductionOrderModel
                {
                    ProductionID = Convert.ToInt32(r["ProductionID"]),
                    OrderCode = r["OrderCode"]?.ToString(),
                    ProductionType = r["ProductionType"]?.ToString(),
                    BOMID = r["BOMID"] != DBNull.Value ? (int?)Convert.ToInt32(r["BOMID"]) : null,
                    FinishedProductID = Convert.ToInt32(r["FinishedProductID"]),
                    FinishedProductCode = r["FinishedProductCode"]?.ToString(),
                    FinishedProductName = r["FinishedProductName"]?.ToString(),
                    ProducedQty = Convert.ToDecimal(r["ProducedQty"]),
                    UnitName = r["UnitName"]?.ToString(),
                    WarehouseID = Convert.ToInt32(r["WarehouseID"]),
                    WarehouseName = r["WarehouseName"]?.ToString(),
                    RawMaterialsCost = Convert.ToDecimal(r["RawMaterialsCost"]),
                    ExtraExpenses = Convert.ToDecimal(r["ExtraExpenses"]),
                    ExpensesNotes = r["ExpensesNotes"]?.ToString(),
                    Status = r["Status"]?.ToString(),
                    StockDeducted = Convert.ToBoolean(r["StockDeducted"]),
                    StockAdded = Convert.ToBoolean(r["StockAdded"]),
                    CreatedDate = Convert.ToDateTime(r["CreatedDate"]),
                    UpdatedDate = Convert.ToDateTime(r["UpdatedDate"]),
                    CompletedDate = r["CompletedDate"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(r["CompletedDate"]) : null,
                    CreatedBy = r["CreatedBy"] != DBNull.Value ? (int?)Convert.ToInt32(r["CreatedBy"]) : null,
                    CreatedByName = r["CreatedByName"]?.ToString(),
                    Notes = r["Notes"]?.ToString()
                };

                var dtItems = DbHelper.Query(@"
                    SELECT poi.*, p.ProductCode AS RawProductCode, p.ProductName AS RawProductName
                    FROM ProductionOrderItems poi
                    JOIN Products p ON poi.RawProductID = p.ProductID
                    WHERE poi.ProductionID = @pid
                    ORDER BY poi.ItemID ASC",
                    DbHelper.P("@pid", order.ProductionID));

                if (dtItems != null)
                {
                    foreach (DataRow ir in dtItems.Rows)
                    {
                        order.Items.Add(new ProductionOrderItemModel
                        {
                            ItemID = Convert.ToInt32(ir["ItemID"]),
                            ProductionID = order.ProductionID,
                            RawProductID = Convert.ToInt32(ir["RawProductID"]),
                            RawProductCode = ir["RawProductCode"]?.ToString(),
                            RawProductName = ir["RawProductName"]?.ToString(),
                            Quantity = Convert.ToDecimal(ir["Quantity"]),
                            UnitCost = Convert.ToDecimal(ir["UnitCost"]),
                            UnitName = ir["UnitName"]?.ToString(),
                            Factor = Convert.ToDecimal(ir["Factor"] == DBNull.Value ? 1 : ir["Factor"]),
                            Notes = ir["Notes"]?.ToString()
                        });
                    }
                }

                return order;
            }
            catch (Exception ex)
            {
                AppLogger.Error("ProductionDAL.GetProductionOrderByID", ex);
                return null;
            }
        }

        public static DataTable GetSuspendedOrders(string prodType = null)
        {
            try
            {
                string sql = @"
                    SELECT po.ProductionID, po.OrderCode, po.ProductionType,
                           CASE WHEN po.ProductionType = 'Fixed' THEN N'تصنيع ثابت' ELSE N'تصنيع مخصص' END AS ProductionTypeName,
                           p.ProductCode, p.ProductName, po.ProducedQty, po.UnitName,
                           po.TotalCost, po.UnitCost, po.CreatedDate, po.UpdatedDate,
                           w.WarehouseName, e.FullName AS CreatedByName,
                           (SELECT COUNT(1) FROM ProductionOrderItems WHERE ProductionID = po.ProductionID) AS ItemsCount
                    FROM ProductionOrders po
                    JOIN Products p ON po.FinishedProductID = p.ProductID
                    LEFT JOIN Warehouses w ON po.WarehouseID = w.WarehouseID
                    LEFT JOIN Employees e ON po.CreatedBy = e.EmpID
                    WHERE po.Status = 'InPreparation'";

                if (!string.IsNullOrWhiteSpace(prodType))
                {
                    sql += " AND po.ProductionType = @type";
                    return DbHelper.Query(sql + " ORDER BY po.UpdatedDate DESC, po.ProductionID DESC", DbHelper.P("@type", prodType));
                }
                return DbHelper.Query(sql + " ORDER BY po.UpdatedDate DESC, po.ProductionID DESC");
            }
            catch (Exception ex)
            {
                AppLogger.Error("ProductionDAL.GetSuspendedOrders", ex);
                return new DataTable();
            }
        }

        public static bool CancelProductionOrder(int productionId, string actionBy, string reason)
        {
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var trans = conn.BeginTransaction())
                {
                    try
                    {
                        var dt = DbHelper.QueryTrans(trans,
                            "SELECT Status, WarehouseID, StockDeducted, StockAdded FROM ProductionOrders WHERE ProductionID = @id",
                            DbHelper.P("@id", productionId));

                        if (dt == null || dt.Rows.Count == 0) return false;
                        string status = dt.Rows[0]["Status"]?.ToString();
                        int wid = Convert.ToInt32(dt.Rows[0]["WarehouseID"]);
                        bool deducted = Convert.ToBoolean(dt.Rows[0]["StockDeducted"]);
                        bool added = Convert.ToBoolean(dt.Rows[0]["StockAdded"]);

                        if (status == "Cancelled") return true;

                        if (deducted && !added)
                        {
                            var dtItems = DbHelper.QueryTrans(trans,
                                "SELECT RawProductID, Quantity FROM ProductionOrderItems WHERE ProductionID = @id",
                                DbHelper.P("@id", productionId));

                            if (dtItems != null)
                            {
                                foreach (DataRow r in dtItems.Rows)
                                {
                                    int pid = Convert.ToInt32(r["RawProductID"]);
                                    decimal qty = Convert.ToDecimal(r["Quantity"]);
                                    AdjustStock(trans, pid, wid, qty);
                                }
                            }
                        }

                        DbHelper.ExecuteTrans(trans, @"
                            UPDATE ProductionOrders
                            SET Status = 'Cancelled', UpdatedDate = GETDATE(), UpdatedBy = @uby,
                                Notes = ISNULL(Notes, '') + ' [تم الإلغاء: ' + @reason + ']'
                            WHERE ProductionID = @id",
                            DbHelper.P("@uby", Session.EmpID),
                            DbHelper.P("@reason", reason ?? "بدون سبب"),
                            DbHelper.P("@id", productionId));

                        AddHistoryTrans(trans, productionId, "Cancelled", actionBy,
                            $"تم إلغاء أمر التصنيع واسترجاع كافة المواد الخام إلى المخزن. السبب: {reason}");

                        trans.Commit();
                        ProductCache.Refresh();
                        return true;
                    }
                    catch
                    {
                        trans.Rollback();
                        throw;
                    }
                }
            }
        }

        public static DataTable SearchProductionOrders(DateTime fromDate, DateTime toDate, string prodType, string status, int? finishedProductId, int? warehouseId, string search)
        {
            try
            {
                string sql = @"
                    SELECT po.ProductionID, po.OrderCode, po.ProductionType,
                           CASE WHEN po.ProductionType = 'Fixed' THEN N'تصنيع ثابت' ELSE N'تصنيع مخصص' END AS ProductionTypeName,
                           p.ProductCode AS FinishedProductCode, p.ProductName AS FinishedProductName,
                           po.ProducedQty, po.UnitName,
                           po.RawMaterialsCost, po.ExtraExpenses, po.TotalCost, po.UnitCost,
                           po.Status,
                           CASE 
                                WHEN po.Status = 'InPreparation' THEN N'تحت التحضير (معلقة)'
                                WHEN po.Status = 'Completed' THEN N'مكتمل ومرحل'
                                WHEN po.Status = 'Cancelled' THEN N'ملغي'
                                ELSE po.Status
                           END AS StatusName,
                           po.CreatedDate, po.UpdatedDate, po.CompletedDate,
                           w.WarehouseName, e.FullName AS CreatedByName, po.Notes
                    FROM ProductionOrders po
                    JOIN Products p ON po.FinishedProductID = p.ProductID
                    LEFT JOIN Warehouses w ON po.WarehouseID = w.WarehouseID
                    LEFT JOIN Employees e ON po.CreatedBy = e.EmpID
                    WHERE CAST(po.CreatedDate AS DATE) >= @from AND CAST(po.CreatedDate AS DATE) <= @to";

                var pars = new List<SqlParameter>
                {
                    DbHelper.P("@from", fromDate.Date),
                    DbHelper.P("@to", toDate.Date)
                };

                if (!string.IsNullOrWhiteSpace(prodType) && prodType != "All")
                {
                    sql += " AND po.ProductionType = @type";
                    pars.Add(DbHelper.P("@type", prodType));
                }

                if (!string.IsNullOrWhiteSpace(status) && status != "All")
                {
                    sql += " AND po.Status = @st";
                    pars.Add(DbHelper.P("@st", status));
                }

                if (finishedProductId.HasValue && finishedProductId.Value > 0)
                {
                    sql += " AND po.FinishedProductID = @fpid";
                    pars.Add(DbHelper.P("@fpid", finishedProductId.Value));
                }

                if (warehouseId.HasValue && warehouseId.Value > 0)
                {
                    sql += " AND po.WarehouseID = @wid";
                    pars.Add(DbHelper.P("@wid", warehouseId.Value));
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    sql += " AND (po.OrderCode LIKE @q OR p.ProductName LIKE @q OR p.ProductCode LIKE @q OR po.Notes LIKE @q)";
                    pars.Add(DbHelper.P("@q", "%" + search.Trim() + "%"));
                }

                sql += " ORDER BY po.CreatedDate DESC, po.ProductionID DESC";
                return DbHelper.Query(sql, pars.ToArray());
            }
            catch (Exception ex)
            {
                AppLogger.Error("ProductionDAL.SearchProductionOrders", ex);
                return new DataTable();
            }
        }

        public static DataTable GetOrderHistory(int productionId)
        {
            try
            {
                return DbHelper.Query(@"
                    SELECT HistoryID, ProductionID, ActionType,
                           CASE 
                               WHEN ActionType = 'Created_Pending' THEN N'إنشاء وتعليق (تحت التحضير)'
                               WHEN ActionType = 'Updated_Pending' THEN N'تعديل وإعادة تعليق'
                               WHEN ActionType = 'Completed' THEN N'إتمام وترحيل التصنيع'
                               WHEN ActionType = 'Cancelled' THEN N'إلغاء أمر التصنيع'
                               ELSE ActionType
                           END AS ActionTypeName,
                           ActionDate, ActionBy, Details
                    FROM ProductionOrderHistory
                    WHERE ProductionID = @id
                    ORDER BY HistoryID ASC",
                    DbHelper.P("@id", productionId));
            }
            catch (Exception ex)
            {
                AppLogger.Error("ProductionDAL.GetOrderHistory", ex);
                return new DataTable();
            }
        }
        #endregion
    }
}

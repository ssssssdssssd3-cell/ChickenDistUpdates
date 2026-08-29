using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using ChickenDist.Core;

namespace ChickenDist.DAL
{
    public class DuplicateProductInfo
    {
        public int ProductID { get; set; }
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public string Unit1Barcode { get; set; }
        public string Unit2Barcode { get; set; }
        public string ScalePLU { get; set; }
        public string CategoryName { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal SalePrice { get; set; }
        public decimal CurrentStock { get; set; }
        public int SalesCount { get; set; }
        public int PurchasesCount { get; set; }
        public int TotalTransactions { get; set; }
        public bool HasTransactions { get; set; }
        public string DuplicateReason { get; set; }
        public bool IsPrimary { get; set; }
    }

    public static class ProductDuplicateDAL
    {
        /// <summary>
        /// جلب تقرير شامل بجميع الأصناف التي تحتوي على أكواد أو باركودات مكررة
        /// </summary>
        public static DataTable GetDuplicateProductsReport(string filterType = "All", string searchKeyword = null)
        {
            string sql = @"
                WITH DuplicateCodes AS (
                    SELECT ProductCode 
                    FROM Products 
                    WHERE IsActive = 1 AND ProductCode IS NOT NULL AND LTRIM(RTRIM(ProductCode)) <> '' AND ProductCode <> 'AUTO'
                    GROUP BY ProductCode 
                    HAVING COUNT(*) > 1
                ),
                DuplicateBarcodes1 AS (
                    SELECT Unit1Barcode AS Barcode 
                    FROM Products 
                    WHERE IsActive = 1 AND Unit1Barcode IS NOT NULL AND LTRIM(RTRIM(Unit1Barcode)) <> ''
                    GROUP BY Unit1Barcode 
                    HAVING COUNT(*) > 1
                ),
                DuplicateBarcodes2 AS (
                    SELECT Unit2Barcode AS Barcode 
                    FROM Products 
                    WHERE IsActive = 1 AND Unit2Barcode IS NOT NULL AND LTRIM(RTRIM(Unit2Barcode)) <> ''
                    GROUP BY Unit2Barcode 
                    HAVING COUNT(*) > 1
                ),
                DuplicatePLU AS (
                    SELECT ScalePLU 
                    FROM Products 
                    WHERE IsActive = 1 AND ScalePLU IS NOT NULL AND LTRIM(RTRIM(ScalePLU)) <> ''
                    GROUP BY ScalePLU 
                    HAVING COUNT(*) > 1
                ),
                DuplicateNames AS (
                    SELECT ProductName 
                    FROM Products 
                    WHERE IsActive = 1 AND ProductName IS NOT NULL AND LTRIM(RTRIM(ProductName)) <> ''
                    GROUP BY ProductName 
                    HAVING COUNT(*) > 1
                )
                SELECT 
                    p.ProductID,
                    p.ProductCode,
                    p.ProductName,
                    p.Unit1Barcode,
                    p.Unit2Barcode,
                    p.ScalePLU,
                    p.PartNumber,
                    c.CategoryName,
                    p.PurchasePrice,
                    p.SalePrice,
                    p.IsActive,
                    ISNULL((SELECT SUM(Quantity) FROM ProductStock ps WHERE ps.ProductID = p.ProductID), 0) AS CurrentStock,
                    ISNULL((SELECT COUNT(*) FROM SaleItems si WHERE si.ProductID = p.ProductID), 0) AS SalesCount,
                    ISNULL((SELECT COUNT(*) FROM PurchaseItems pi WHERE pi.ProductID = p.ProductID), 0) AS PurchasesCount,
                    ISNULL((SELECT COUNT(*) FROM ReturnItems ri WHERE ri.ProductID = p.ProductID), 0) AS ReturnsCount,
                    (ISNULL((SELECT COUNT(*) FROM SaleItems si WHERE si.ProductID = p.ProductID), 0) + 
                     ISNULL((SELECT COUNT(*) FROM PurchaseItems pi WHERE pi.ProductID = p.ProductID), 0) + 
                     ISNULL((SELECT COUNT(*) FROM ReturnItems ri WHERE ri.ProductID = p.ProductID), 0) + 
                     ISNULL((SELECT COUNT(*) FROM ProductionOrderItems poi WHERE poi.RawProductID = p.ProductID), 0) + 
                     ISNULL((SELECT COUNT(*) FROM ProductionOrders po WHERE po.FinishedProductID = p.ProductID), 0)) AS TotalTransactions,
                    CASE 
                        WHEN (ISNULL((SELECT COUNT(*) FROM SaleItems si WHERE si.ProductID = p.ProductID), 0) + 
                              ISNULL((SELECT COUNT(*) FROM PurchaseItems pi WHERE pi.ProductID = p.ProductID), 0) + 
                              ISNULL((SELECT COUNT(*) FROM ReturnItems ri WHERE ri.ProductID = p.ProductID), 0) + 
                              ISNULL((SELECT COUNT(*) FROM ProductionOrderItems poi WHERE poi.RawProductID = p.ProductID), 0) + 
                              ISNULL((SELECT COUNT(*) FROM ProductionOrders po WHERE po.FinishedProductID = p.ProductID), 0)) > 0
                             OR ABS(ISNULL((SELECT SUM(Quantity) FROM ProductStock ps WHERE ps.ProductID = p.ProductID), 0)) > 0.0001 THEN 1
                        ELSE 0
                    END AS HasTransactions,
                    CASE 
                        WHEN p.ProductCode IN (SELECT ProductCode FROM DuplicateCodes) THEN N'كود الصنف مكرر [' + ISNULL(p.ProductCode, '') + N']'
                        WHEN p.Unit1Barcode IN (SELECT Barcode FROM DuplicateBarcodes1) THEN N'باركود الوحدة 1 مكرر [' + ISNULL(p.Unit1Barcode, '') + N']'
                        WHEN p.Unit2Barcode IN (SELECT Barcode FROM DuplicateBarcodes2) THEN N'باركود الوحدة 2 مكرر [' + ISNULL(p.Unit2Barcode, '') + N']'
                        WHEN p.ScalePLU IN (SELECT ScalePLU FROM DuplicatePLU) THEN N'كود الميزان مكرر [' + ISNULL(p.ScalePLU, '') + N']'
                        WHEN p.ProductName IN (SELECT ProductName FROM DuplicateNames) THEN N'اسم الصنف مكرر بالكامل'
                        ELSE N'تكرار عام'
                    END AS DuplicateReason,
                    CASE 
                        WHEN p.ProductCode IN (SELECT ProductCode FROM DuplicateCodes) THEN p.ProductCode
                        WHEN p.Unit1Barcode IN (SELECT Barcode FROM DuplicateBarcodes1) THEN p.Unit1Barcode
                        WHEN p.Unit2Barcode IN (SELECT Barcode FROM DuplicateBarcodes2) THEN p.Unit2Barcode
                        WHEN p.ScalePLU IN (SELECT ScalePLU FROM DuplicatePLU) THEN p.ScalePLU
                        WHEN p.ProductName IN (SELECT ProductName FROM DuplicateNames) THEN p.ProductName
                        ELSE CAST(p.ProductID AS NVARCHAR(50))
                    END AS GroupKey
                FROM Products p
                LEFT JOIN Categories c ON p.CategoryID = c.CategoryID
                WHERE p.IsActive = 1 AND (
            ";

            if (filterType == "ProductCode")
            {
                sql += " p.ProductCode IN (SELECT ProductCode FROM DuplicateCodes) ";
            }
            else if (filterType == "Barcode")
            {
                sql += " (p.Unit1Barcode IN (SELECT Barcode FROM DuplicateBarcodes1) OR p.Unit2Barcode IN (SELECT Barcode FROM DuplicateBarcodes2)) ";
            }
            else if (filterType == "ScalePLU")
            {
                sql += " p.ScalePLU IN (SELECT ScalePLU FROM DuplicatePLU) ";
            }
            else if (filterType == "ProductName")
            {
                sql += " p.ProductName IN (SELECT ProductName FROM DuplicateNames) ";
            }
            else // All
            {
                sql += @" (p.ProductCode IN (SELECT ProductCode FROM DuplicateCodes)
                           OR p.Unit1Barcode IN (SELECT Barcode FROM DuplicateBarcodes1)
                           OR p.Unit2Barcode IN (SELECT Barcode FROM DuplicateBarcodes2)
                           OR p.ScalePLU IN (SELECT ScalePLU FROM DuplicatePLU)) ";
            }

            sql += " ) ";

            var pars = new List<SqlParameter>();
            if (!string.IsNullOrWhiteSpace(searchKeyword))
            {
                sql += " AND (p.ProductName LIKE @q OR p.ProductCode LIKE @q OR p.Unit1Barcode LIKE @q) ";
                pars.Add(DbHelper.P("@q", "%" + searchKeyword.Trim() + "%"));
            }

            sql += @" ORDER BY GroupKey ASC, 
                              HasTransactions DESC, 
                              TotalTransactions DESC, 
                              (CASE WHEN CurrentStock > 0 THEN 1 ELSE 0 END) DESC, 
                              p.ProductID ASC";

            return DbHelper.Query(sql, pars.ToArray());
        }

        /// <summary>
        /// حل تلقائي ذكي لجميع الأكواد المكررة:
        /// الاحتفاظ بأول صنف (الأكثر حركة أو أقدمية) وإعادة ترقيم الأصناف المكررة الأخرى بأكواد فريدة جديدة
        /// </summary>
        public static (int totalFixed, List<string> fixLog) AutoFixDuplicateProductCodes(string scope = "ProductCode", bool onlyModifyZeroTransactions = true)
        {
            DataTable dt = GetDuplicateProductsReport(scope);
            var fixLog = new List<string>();
            int fixedCount = 0;

            if (dt.Rows.Count == 0) return (0, fixLog);

            // تجميع الأصناف حسب كود التكرار
            var groups = new Dictionary<string, List<DataRow>>(StringComparer.OrdinalIgnoreCase);
            foreach (DataRow r in dt.Rows)
            {
                string key = r["GroupKey"].ToString().Trim();
                if (!groups.ContainsKey(key)) groups[key] = new List<DataRow>();
                groups[key].Add(r);
            }

            DbHelper.RunInTransaction((con, trans) =>
            {
                foreach (var kvp in groups)
                {
                    var rows = kvp.Value;
                    if (rows.Count <= 1) continue;
                    string origCode = kvp.Key;

                    var withTrans = new List<DataRow>();
                    var withoutTrans = new List<DataRow>();

                    foreach (var r in rows)
                    {
                        int hasT = Convert.ToInt32(r["HasTransactions"]);
                        if (hasT == 1) withTrans.Add(r);
                        else withoutTrans.Add(r);
                    }

                    // الحالة 1: يوجد صنف له حركات أو رصيد
                    if (withTrans.Count > 0)
                    {
                        var primaryRow = withTrans[0];
                        int primaryID = Convert.ToInt32(primaryRow["ProductID"]);
                        string primaryName = primaryRow["ProductName"].ToString();
                        int pTrans = Convert.ToInt32(primaryRow["TotalTransactions"]);
                        decimal pStock = Convert.ToDecimal(primaryRow["CurrentStock"]);

                        fixLog.Add($"📌 كود الصنف [{origCode}]: تم الاحتفاظ به للصنف [ID: {primaryID} - {primaryName}] لوجود حركات/رصيد مسجل ({pTrans} حركة، رصيد: {pStock:N2}).");

                        // تعديل كود الأصناف التي ليس لها أي حركات فقط
                        for (int i = 0; i < withoutTrans.Count; i++)
                        {
                            var dupRow = withoutTrans[i];
                            int dupID = Convert.ToInt32(dupRow["ProductID"]);
                            string dupName = dupRow["ProductName"].ToString();

                            string newCode = GenerateUniqueProductCodeTrans(trans, origCode, i + 1);

                            DbHelper.ExecuteTrans(trans,
                                "UPDATE Products SET ProductCode = @nc WHERE ProductID = @id",
                                DbHelper.P("@nc", newCode), DbHelper.P("@id", dupID));

                            fixLog.Add($"   ⚡ تم تعديل كود الصنف [ID: {dupID} - {dupName}] (بدون أي حركات أو رصيد) من [{origCode}] إلى الكود الجديد الفريد [{newCode}]");
                            fixedCount++;
                        }

                        // إذا كان هناك أصناف أخرى لها حركات أيضاً
                        if (withTrans.Count > 1)
                        {
                            for (int i = 1; i < withTrans.Count; i++)
                            {
                                var otherRow = withTrans[i];
                                int oID = Convert.ToInt32(otherRow["ProductID"]);
                                string oName = otherRow["ProductName"].ToString();
                                int oTrans = Convert.ToInt32(otherRow["TotalTransactions"]);
                                decimal oStock = Convert.ToDecimal(otherRow["CurrentStock"]);

                                if (onlyModifyZeroTransactions)
                                {
                                    fixLog.Add($"   🛡️ الصنف [ID: {oID} - {oName}]: تم تخطيه وحمايته من تعديل الكود لأن له حركات/رصيد مسجل ({oTrans} حركة، رصيد: {oStock:N2}).");
                                }
                                else
                                {
                                    string newCode = GenerateUniqueProductCodeTrans(trans, origCode, i + 10);
                                    DbHelper.ExecuteTrans(trans,
                                        "UPDATE Products SET ProductCode = @nc WHERE ProductID = @id",
                                        DbHelper.P("@nc", newCode), DbHelper.P("@id", oID));

                                    fixLog.Add($"   ⚡ تم تعديل كود الصنف [ID: {oID} - {oName}] من [{origCode}] إلى [{newCode}]");
                                    fixedCount++;
                                }
                            }
                        }
                    }
                    else
                    {
                        // الحالة 2: جميع الأصناف المشتركة في الكود ليس لها أي حركات على الإطلاق (0 حركات)
                        var primaryRow = withoutTrans[0];
                        int primaryID = Convert.ToInt32(primaryRow["ProductID"]);
                        string primaryName = primaryRow["ProductName"].ToString();

                        fixLog.Add($"📌 كود الصنف [{origCode}]: تم الاحتفاظ به للصنف الأقدم [ID: {primaryID} - {primaryName}] (بدون حركات).");

                        for (int i = 1; i < withoutTrans.Count; i++)
                        {
                            var dupRow = withoutTrans[i];
                            int dupID = Convert.ToInt32(dupRow["ProductID"]);
                            string dupName = dupRow["ProductName"].ToString();

                            string newCode = GenerateUniqueProductCodeTrans(trans, origCode, i);

                            DbHelper.ExecuteTrans(trans,
                                "UPDATE Products SET ProductCode = @nc WHERE ProductID = @id",
                                DbHelper.P("@nc", newCode), DbHelper.P("@id", dupID));

                            fixLog.Add($"   ⚡ تم تعديل كود الصنف [ID: {dupID} - {dupName}] (بدون أي حركات) من [{origCode}] إلى الكود الجديد الفريد [{newCode}]");
                            fixedCount++;
                        }
                    }
                }
            });

            ProductCache.Invalidate();
            return (fixedCount, fixLog);
        }

        /// <summary>
        /// حل تلقائي للباركودات المكررة (تفريغ الباركود المكرر من الصنف الثانوي لمنع تعارض قارئ الباركود)
        /// </summary>
        public static (int totalFixed, List<string> fixLog) AutoFixDuplicateBarcodes()
        {
            DataTable dt = GetDuplicateProductsReport("Barcode");
            var fixLog = new List<string>();
            int fixedCount = 0;

            var groups = new Dictionary<string, List<DataRow>>(StringComparer.OrdinalIgnoreCase);
            foreach (DataRow r in dt.Rows)
            {
                string key = r["GroupKey"].ToString().Trim();
                if (!groups.ContainsKey(key)) groups[key] = new List<DataRow>();
                groups[key].Add(r);
            }

            DbHelper.RunInTransaction((con, trans) =>
            {
                foreach (var kvp in groups)
                {
                    var rows = kvp.Value;
                    if (rows.Count <= 1) continue;

                    var primaryRow = rows[0];
                    int primaryID = Convert.ToInt32(primaryRow["ProductID"]);
                    string barcode = kvp.Key;

                    fixLog.Add($"🏷️ الباركود [{barcode}]: تم إبقاؤه للصنف [ID: {primaryID} - {primaryRow["ProductName"]}]");

                    for (int i = 1; i < rows.Count; i++)
                    {
                        var dupRow = rows[i];
                        int dupID = Convert.ToInt32(dupRow["ProductID"]);

                        // تفريغ الباركود المكرر لمنع التضارب في أجهزة السكنر
                        if (dupRow["Unit1Barcode"] != DBNull.Value && dupRow["Unit1Barcode"].ToString().Trim() == barcode)
                        {
                            DbHelper.ExecuteTrans(trans, "UPDATE Products SET Unit1Barcode = NULL WHERE ProductID = @id", DbHelper.P("@id", dupID));
                            fixLog.Add($"   ⚡ تم إزالة باركود الوحدة 1 المكرر من الصنف [ID: {dupID} - {dupRow["ProductName"]}]");
                            fixedCount++;
                        }
                        if (dupRow["Unit2Barcode"] != DBNull.Value && dupRow["Unit2Barcode"].ToString().Trim() == barcode)
                        {
                            DbHelper.ExecuteTrans(trans, "UPDATE Products SET Unit2Barcode = NULL WHERE ProductID = @id", DbHelper.P("@id", dupID));
                            fixLog.Add($"   ⚡ تم إزالة باركود الوحدة 2 المكرر من الصنف [ID: {dupID} - {dupRow["ProductName"]}]");
                            fixedCount++;
                        }
                    }
                }
            });

            ProductCache.Invalidate();
            return (fixedCount, fixLog);
        }

        /// <summary>
        /// دمج صنفين مكررين في صنف واحد بالكامل مع ترحيل المخزون والفواتير والحركات
        /// </summary>
        public static bool MergeDuplicateProducts(int targetProductID, int sourceProductID, out string error)
        {
            error = "";
            if (targetProductID == sourceProductID)
            {
                error = "لا يمكن دمج الصنف في نفسه!";
                return false;
            }

            try
            {
                DbHelper.RunInTransaction((con, trans) =>
                {
                    // 1. تحويل كميات المخزون في WarehouseStock
                    var dtSourceStock = DbHelper.QueryTrans(trans,
                        "SELECT WarehouseID, Quantity FROM WarehouseStock WHERE ProductID = @pid",
                        DbHelper.P("@pid", sourceProductID));

                    foreach (DataRow sr in dtSourceStock.Rows)
                    {
                        int wid = Convert.ToInt32(sr["WarehouseID"]);
                        decimal qty = Convert.ToDecimal(sr["Quantity"]);

                        if (qty != 0)
                        {
                            // فحص هل الصنف الهدف له سجل في هذا المخزن؟
                            var exists = DbHelper.ScalarTrans(trans,
                                "SELECT COUNT(*) FROM WarehouseStock WHERE ProductID = @pid AND WarehouseID = @wid",
                                DbHelper.P("@pid", targetProductID), DbHelper.P("@wid", wid));

                            if (exists != null && Convert.ToInt32(exists) > 0)
                            {
                                DbHelper.ExecuteTrans(trans,
                                    "UPDATE WarehouseStock SET Quantity = Quantity + @q WHERE ProductID = @pid AND WarehouseID = @wid",
                                    DbHelper.P("@q", qty), DbHelper.P("@pid", targetProductID), DbHelper.P("@wid", wid));
                            }
                            else
                            {
                                DbHelper.ExecuteTrans(trans,
                                    "INSERT INTO WarehouseStock (ProductID, WarehouseID, Quantity) VALUES (@pid, @wid, @q)",
                                    DbHelper.P("@pid", targetProductID), DbHelper.P("@wid", wid), DbHelper.P("@q", qty));
                            }
                        }
                    }
                    DbHelper.ExecuteTrans(trans, "DELETE FROM WarehouseStock WHERE ProductID = @pid", DbHelper.P("@pid", sourceProductID));

                    // 2. تحويل فواتير المبيعات
                    DbHelper.ExecuteTrans(trans, "UPDATE SaleItems SET ProductID = @target WHERE ProductID = @src",
                        DbHelper.P("@target", targetProductID), DbHelper.P("@src", sourceProductID));

                    if (TableExistsTrans(trans, "SaleItemsHistory"))
                    {
                        DbHelper.ExecuteTrans(trans, "UPDATE SaleItemsHistory SET ProductID = @target WHERE ProductID = @src",
                            DbHelper.P("@target", targetProductID), DbHelper.P("@src", sourceProductID));
                    }

                    // 3. تحويل فواتير المشتريات
                    DbHelper.ExecuteTrans(trans, "UPDATE PurchaseItems SET ProductID = @target WHERE ProductID = @src",
                        DbHelper.P("@target", targetProductID), DbHelper.P("@src", sourceProductID));

                    // 4. تحويل مرتجعات المبيعات والمشتريات
                    if (TableExistsTrans(trans, "SalesReturnsItems"))
                    {
                        DbHelper.ExecuteTrans(trans, "UPDATE SalesReturnsItems SET ProductID = @target WHERE ProductID = @src",
                            DbHelper.P("@target", targetProductID), DbHelper.P("@src", sourceProductID));
                    }
                    if (TableExistsTrans(trans, "PurchaseReturnsItems"))
                    {
                        DbHelper.ExecuteTrans(trans, "UPDATE PurchaseReturnsItems SET ProductID = @target WHERE ProductID = @src",
                            DbHelper.P("@target", targetProductID), DbHelper.P("@src", sourceProductID));
                    }

                    // 5. تحويل الباتشات والتواريخ
                    if (TableExistsTrans(trans, "ProductBatches"))
                    {
                        DbHelper.ExecuteTrans(trans, "UPDATE ProductBatches SET ProductID = @target WHERE ProductID = @src",
                            DbHelper.P("@target", targetProductID), DbHelper.P("@src", sourceProductID));
                    }

                    // 6. تحويل التحويلات المخزنية والهوالك
                    if (TableExistsTrans(trans, "WarehouseTransferItems"))
                    {
                        DbHelper.ExecuteTrans(trans, "UPDATE WarehouseTransferItems SET ProductID = @target WHERE ProductID = @src",
                            DbHelper.P("@target", targetProductID), DbHelper.P("@src", sourceProductID));
                    }
                    if (TableExistsTrans(trans, "Wastage"))
                    {
                        DbHelper.ExecuteTrans(trans, "UPDATE Wastage SET ProductID = @target WHERE ProductID = @src",
                            DbHelper.P("@target", targetProductID), DbHelper.P("@src", sourceProductID));
                    }

                    // 7. تحويل عمولات الأصناف وسجل الأسعار والنواقص
                    if (TableExistsTrans(trans, "EmployeeProductCommissions"))
                    {
                        DbHelper.ExecuteTrans(trans, "UPDATE EmployeeProductCommissions SET ProductID = @target WHERE ProductID = @src",
                            DbHelper.P("@target", targetProductID), DbHelper.P("@src", sourceProductID));
                    }
                    if (TableExistsTrans(trans, "PriceChangesLog"))
                    {
                        DbHelper.ExecuteTrans(trans, "UPDATE PriceChangesLog SET ProductID = @target WHERE ProductID = @src",
                            DbHelper.P("@target", targetProductID), DbHelper.P("@src", sourceProductID));
                    }
                    if (TableExistsTrans(trans, "ShortageNotebook"))
                    {
                        DbHelper.ExecuteTrans(trans, "UPDATE ShortageNotebook SET ProductID = @target WHERE ProductID = @src",
                            DbHelper.P("@target", targetProductID), DbHelper.P("@src", sourceProductID));
                    }

                    // 8. محاولة حذف الصنف المصدر بالكامل، وإذا تعذر نقوم بتعطيله وتغيير كوده
                    try
                    {
                        DbHelper.ExecuteTrans(trans, "DELETE FROM Products WHERE ProductID = @src", DbHelper.P("@src", sourceProductID));
                    }
                    catch
                    {
                        DbHelper.ExecuteTrans(trans,
                            @"UPDATE Products 
                              SET IsActive = 0, 
                                  ProductCode = ProductCode + '_MERGED_' + CAST(ProductID AS NVARCHAR(20)),
                                  Unit1Barcode = NULL, Unit2Barcode = NULL, ScalePLU = NULL,
                                  ProductName = ProductName + N' (مدمج مع #' + CAST(@target AS NVARCHAR(20)) + N')'
                              WHERE ProductID = @src",
                            DbHelper.P("@target", targetProductID), DbHelper.P("@src", sourceProductID));
                    }
                });

                ProductCache.Invalidate();
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// تعديل كود صنف يدوياً مع التحقق من عدم وجود تكرار
        /// </summary>
        public static bool UpdateProductCodeDirect(int productID, string newCode, out string error)
        {
            error = "";
            if (string.IsNullOrWhiteSpace(newCode))
            {
                error = "الكود لا يمكن أن يكون فارغاً.";
                return false;
            }

            newCode = newCode.Trim();

            // فحص هل الكود مستخدم لصنف آخر نشط؟
            var exists = DbHelper.Scalar(
                "SELECT COUNT(*) FROM Products WHERE IsActive = 1 AND ProductCode = @c AND ProductID <> @id",
                DbHelper.P("@c", newCode), DbHelper.P("@id", productID));

            if (exists != null && Convert.ToInt32(exists) > 0)
            {
                error = $"الكود [{newCode}] مستخدم بالفعل لصنف آخر!";
                return false;
            }

            DbHelper.Execute("UPDATE Products SET ProductCode = @c WHERE ProductID = @id",
                DbHelper.P("@c", newCode), DbHelper.P("@id", productID));

            ProductCache.Invalidate();
            return true;
        }

        private static string GenerateUniqueProductCodeTrans(SqlTransaction trans, string baseCode, int suffix)
        {
            // إذا كان الكود الأصلي رقماً خالصاً، نولد كوداً رقمياً متسلسلاً نظيفاً
            if (long.TryParse(baseCode, out long _))
            {
                int maxCode = 0;
                try
                {
                    var resCode = DbHelper.ScalarTrans(trans, @"
                        SELECT COALESCE(MAX(CASE 
                            WHEN ISNUMERIC(ProductCode) = 1 AND LEN(ProductCode) <= 9 AND ProductCode NOT LIKE '%.%' AND ProductCode NOT LIKE '%-%' AND ProductCode NOT LIKE '%+%'
                            THEN CAST(ProductCode AS INT) 
                            ELSE 0 
                        END), 0) FROM Products");
                    if (resCode != null && resCode != DBNull.Value) maxCode = Convert.ToInt32(resCode);
                }
                catch { }

                try
                {
                    var resId = DbHelper.ScalarTrans(trans, "SELECT COALESCE(MAX(ProductID), 0) FROM Products");
                    if (resId != null && resId != DBNull.Value)
                    {
                        int maxId = Convert.ToInt32(resId);
                        if (maxId > maxCode) maxCode = maxId;
                    }
                }
                catch { }

                int nextNum = Math.Max(maxCode + 1, 1001);
                string candidateNum = nextNum.ToString();
                while (Convert.ToInt32(DbHelper.ScalarTrans(trans, "SELECT COUNT(1) FROM Products WHERE ProductCode = @c", DbHelper.P("@c", candidateNum))) > 0)
                {
                    nextNum++;
                    candidateNum = nextNum.ToString();
                }
                return candidateNum;
            }

            string candidate = $"{baseCode}-{suffix}";

            // التحقق من أن الكود المقترح غير موجود في قاعدة البيانات
            var count = DbHelper.ScalarTrans(trans,
                "SELECT COUNT(*) FROM Products WHERE ProductCode = @c", DbHelper.P("@c", candidate));

            if (count == null || Convert.ToInt32(count) == 0) return candidate;

            // إذا كان موجوداً نولد كود تسلسلي فريد
            int extra = suffix;
            while (true)
            {
                extra++;
                candidate = $"{baseCode}-{extra}";
                var c2 = DbHelper.ScalarTrans(trans, "SELECT COUNT(*) FROM Products WHERE ProductCode = @c", DbHelper.P("@c", candidate));
                if (c2 == null || Convert.ToInt32(c2) == 0) return candidate;
            }
        }

        private static bool TableExistsTrans(SqlTransaction trans, string tableName)
        {
            var res = DbHelper.ScalarTrans(trans,
                "SELECT COUNT(*) FROM sys.tables WHERE name = @t", DbHelper.P("@t", tableName));
            return res != null && Convert.ToInt32(res) > 0;
        }
    }
}

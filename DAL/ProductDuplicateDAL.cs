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
            bool hasProductStock = DbHelper.TableExists("ProductStock");
            bool hasWarehouseStock = !hasProductStock && DbHelper.TableExists("WarehouseStock");
            string stockSubquery = hasProductStock 
                ? "ISNULL((SELECT SUM(Quantity) FROM ProductStock ps WHERE ps.ProductID = p.ProductID), 0)" 
                : (hasWarehouseStock ? "ISNULL((SELECT SUM(Quantity) FROM WarehouseStock ws WHERE ws.ProductID = p.ProductID), 0)" : "0");

            bool hasSales = DbHelper.TableExists("SaleItems");
            string salesSubquery = hasSales ? "ISNULL((SELECT COUNT(*) FROM SaleItems si WHERE si.ProductID = p.ProductID), 0)" : "0";

            bool hasPurchases = DbHelper.TableExists("PurchaseItems");
            string purchasesSubquery = hasPurchases ? "ISNULL((SELECT COUNT(*) FROM PurchaseItems pi WHERE pi.ProductID = p.ProductID), 0)" : "0";

            bool hasReturns = DbHelper.TableExists("ReturnItems");
            string returnsSubquery = hasReturns ? "ISNULL((SELECT COUNT(*) FROM ReturnItems ri WHERE ri.ProductID = p.ProductID), 0)" : "0";

            bool hasProdItems = DbHelper.TableExists("ProductionOrderItems");
            string prodItemsSubquery = hasProdItems ? "ISNULL((SELECT COUNT(*) FROM ProductionOrderItems poi WHERE poi.RawProductID = p.ProductID), 0)" : "0";

            bool hasProdOrders = DbHelper.TableExists("ProductionOrders");
            string prodOrdersSubquery = hasProdOrders ? "ISNULL((SELECT COUNT(*) FROM ProductionOrders po WHERE po.FinishedProductID = p.ProductID), 0)" : "0";

            bool hasBarcode1 = DbHelper.ColumnExists("Products", "Unit1Barcode");
            bool hasBarcode2 = DbHelper.ColumnExists("Products", "Unit2Barcode");
            bool hasScalePLU = DbHelper.ColumnExists("Products", "ScalePLU");
            bool hasPartNumber = DbHelper.ColumnExists("Products", "PartNumber");
            bool hasCategory = DbHelper.TableExists("Categories") && DbHelper.ColumnExists("Products", "CategoryID");

            string colBarcode1 = hasBarcode1 ? "p.Unit1Barcode" : "CAST(NULL AS NVARCHAR(50)) AS Unit1Barcode";
            string colBarcode2 = hasBarcode2 ? "p.Unit2Barcode" : "CAST(NULL AS NVARCHAR(50)) AS Unit2Barcode";
            string colScalePLU = hasScalePLU ? "p.ScalePLU" : "CAST(NULL AS NVARCHAR(50)) AS ScalePLU";
            string colPartNum = hasPartNumber ? "p.PartNumber" : "CAST(NULL AS NVARCHAR(50)) AS PartNumber";
            string colCategory = hasCategory ? "c.CategoryName" : "N'عام' AS CategoryName";
            string joinCategory = hasCategory ? "LEFT JOIN Categories c ON p.CategoryID = c.CategoryID" : "";

            string sql = $@"
                WITH DuplicateCodes AS (
                    SELECT LTRIM(RTRIM(ProductCode)) AS ProductCode 
                    FROM Products 
                    WHERE IsActive = 1 AND ProductCode IS NOT NULL AND LTRIM(RTRIM(ProductCode)) <> '' AND ProductCode <> 'AUTO'
                    GROUP BY LTRIM(RTRIM(ProductCode)) 
                    HAVING COUNT(*) > 1
                ),
                DuplicateBarcodes1 AS (
                    SELECT {(hasBarcode1 ? "Unit1Barcode" : "NULL")} AS Barcode 
                    FROM Products 
                    WHERE IsActive = 1 AND {(hasBarcode1 ? "Unit1Barcode IS NOT NULL AND LTRIM(RTRIM(Unit1Barcode)) <> ''" : "1 = 0")}
                    GROUP BY {(hasBarcode1 ? "Unit1Barcode" : "NULL")} 
                    HAVING COUNT(*) > 1
                ),
                DuplicateBarcodes2 AS (
                    SELECT {(hasBarcode2 ? "Unit2Barcode" : "NULL")} AS Barcode 
                    FROM Products 
                    WHERE IsActive = 1 AND {(hasBarcode2 ? "Unit2Barcode IS NOT NULL AND LTRIM(RTRIM(Unit2Barcode)) <> ''" : "1 = 0")}
                    GROUP BY {(hasBarcode2 ? "Unit2Barcode" : "NULL")} 
                    HAVING COUNT(*) > 1
                ),
                DuplicatePLU AS (
                    SELECT {(hasScalePLU ? "ScalePLU" : "NULL")} AS ScalePLU 
                    FROM Products 
                    WHERE IsActive = 1 AND {(hasScalePLU ? "ScalePLU IS NOT NULL AND LTRIM(RTRIM(ScalePLU)) <> ''" : "1 = 0")}
                    GROUP BY {(hasScalePLU ? "ScalePLU" : "NULL")} 
                    HAVING COUNT(*) > 1
                ),
                DuplicateNames AS (
                    SELECT ProductName 
                    FROM Products 
                    WHERE IsActive = 1 AND ProductName IS NOT NULL AND LTRIM(RTRIM(ProductName)) <> ''
                    GROUP BY ProductName 
                    HAVING COUNT(*) > 1
                )
                SELECT * FROM (
                    SELECT 
                        p.ProductID,
                        LTRIM(RTRIM(p.ProductCode)) AS ProductCode,
                        p.ProductName,
                        {colBarcode1},
                        {colBarcode2},
                        {colScalePLU},
                        {colPartNum},
                        {colCategory},
                        p.PurchasePrice,
                        p.SalePrice,
                        p.IsActive,
                        {stockSubquery} AS CurrentStock,
                        {salesSubquery} AS SalesCount,
                        {purchasesSubquery} AS PurchasesCount,
                        {returnsSubquery} AS ReturnsCount,
                        ({salesSubquery} + {purchasesSubquery} + {returnsSubquery} + {prodItemsSubquery} + {prodOrdersSubquery}) AS TotalTransactions,
                        CASE 
                            WHEN ({salesSubquery} + {purchasesSubquery} + {returnsSubquery} + {prodItemsSubquery} + {prodOrdersSubquery}) > 0
                                 OR ABS({stockSubquery}) > 0.0001 THEN 1
                            ELSE 0
                        END AS HasTransactions,
                        CASE 
                            WHEN LTRIM(RTRIM(p.ProductCode)) IN (SELECT ProductCode FROM DuplicateCodes) THEN N'كود الصنف مكرر [' + ISNULL(LTRIM(RTRIM(p.ProductCode)), '') + N']'
                            WHEN {(hasBarcode1 ? "p.Unit1Barcode IN (SELECT Barcode FROM DuplicateBarcodes1)" : "1=0")} THEN N'باركود الوحدة 1 مكرر [' + ISNULL({(hasBarcode1 ? "p.Unit1Barcode" : "''")}, '') + N']'
                            WHEN {(hasBarcode2 ? "p.Unit2Barcode IN (SELECT Barcode FROM DuplicateBarcodes2)" : "1=0")} THEN N'باركود الوحدة 2 مكرر [' + ISNULL({(hasBarcode2 ? "p.Unit2Barcode" : "''")}, '') + N']'
                            WHEN {(hasScalePLU ? "p.ScalePLU IN (SELECT ScalePLU FROM DuplicatePLU)" : "1=0")} THEN N'كود الميزان مكرر [' + ISNULL({(hasScalePLU ? "p.ScalePLU" : "''")}, '') + N']'
                            WHEN p.ProductName IN (SELECT ProductName FROM DuplicateNames) THEN N'اسم الصنف مكرر بالكامل'
                            ELSE N'تكرار عام'
                        END AS DuplicateReason,
                        CASE 
                            WHEN LTRIM(RTRIM(p.ProductCode)) IN (SELECT ProductCode FROM DuplicateCodes) THEN LTRIM(RTRIM(p.ProductCode))
                            WHEN {(hasBarcode1 ? "p.Unit1Barcode IN (SELECT Barcode FROM DuplicateBarcodes1)" : "1=0")} THEN {(hasBarcode1 ? "p.Unit1Barcode" : "''")}
                            WHEN {(hasBarcode2 ? "p.Unit2Barcode IN (SELECT Barcode FROM DuplicateBarcodes2)" : "1=0")} THEN {(hasBarcode2 ? "p.Unit2Barcode" : "''")}
                            WHEN {(hasScalePLU ? "p.ScalePLU IN (SELECT ScalePLU FROM DuplicatePLU)" : "1=0")} THEN {(hasScalePLU ? "p.ScalePLU" : "''")}
                            WHEN p.ProductName IN (SELECT ProductName FROM DuplicateNames) THEN p.ProductName
                            ELSE CAST(p.ProductID AS NVARCHAR(50))
                        END AS GroupKey
                    FROM Products p
                    {joinCategory}
                    WHERE p.IsActive = 1 AND (
                ";

            if (filterType == "ProductCode")
            {
                sql += " LTRIM(RTRIM(p.ProductCode)) IN (SELECT ProductCode FROM DuplicateCodes) ";
            }
            else if (filterType == "Barcode")
            {
                string bCond = " 1 = 0 ";
                if (hasBarcode1) bCond += " OR p.Unit1Barcode IN (SELECT Barcode FROM DuplicateBarcodes1) ";
                if (hasBarcode2) bCond += " OR p.Unit2Barcode IN (SELECT Barcode FROM DuplicateBarcodes2) ";
                sql += $" ({bCond}) ";
            }
            else if (filterType == "ScalePLU")
            {
                sql += hasScalePLU ? " p.ScalePLU IN (SELECT ScalePLU FROM DuplicatePLU) " : " 1 = 0 ";
            }
            else if (filterType == "ProductName")
            {
                sql += " p.ProductName IN (SELECT ProductName FROM DuplicateNames) ";
            }
            else // All
            {
                sql += @" (LTRIM(RTRIM(p.ProductCode)) IN (SELECT ProductCode FROM DuplicateCodes) ";
                if (hasBarcode1) sql += " OR p.Unit1Barcode IN (SELECT Barcode FROM DuplicateBarcodes1) ";
                if (hasBarcode2) sql += " OR p.Unit2Barcode IN (SELECT Barcode FROM DuplicateBarcodes2) ";
                if (hasScalePLU) sql += " OR p.ScalePLU IN (SELECT ScalePLU FROM DuplicatePLU) ";
                sql += ") ";
            }

            sql += " ) ";

            var pars = new List<SqlParameter>();
            if (!string.IsNullOrWhiteSpace(searchKeyword))
            {
                string trimmedSearch = searchKeyword.Trim();
                sql += " AND (p.ProductName LIKE @q OR LTRIM(RTRIM(p.ProductCode)) LIKE @q OR LTRIM(RTRIM(p.ProductCode)) = @exactQ ";
                if (hasBarcode1) sql += " OR p.Unit1Barcode LIKE @q ";
                sql += ") ";
                pars.Add(DbHelper.P("@q", "%" + trimmedSearch + "%"));
                pars.Add(DbHelper.P("@exactQ", trimmedSearch));
            }

            sql += @" ) t 
                      ORDER BY GroupKey ASC, 
                               HasTransactions DESC, 
                               TotalTransactions DESC, 
                               (CASE WHEN CurrentStock > 0 THEN 1 ELSE 0 END) DESC, 
                               ProductID ASC";

            return DbHelper.Query(sql, pars.ToArray());
        }

        /// <summary>
        /// حل تلقائي ذكي لجميع الأكواد المكررة:
        /// الاحتفاظ بالصنف الأكثر حركة ورصيداً وإعادة ترقيم الأصناف المكررة الأقل حركة بأكواد تسلسلية جديدة مع تحديث كافة حركاتها
        /// </summary>
        public static (int totalFixed, List<string> fixLog) AutoFixDuplicateProductCodes(string scope = "ProductCode", bool onlyModifyZeroTransactions = false)
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
                // استخراج أعلى كود رقمي لبدء الترقيم المتسلسل
                long nextSeq = 0;
                try
                {
                    var resCode = DbHelper.ScalarTrans(trans, @"
                        SELECT COALESCE(MAX(CASE 
                            WHEN ISNUMERIC(ProductCode) = 1 AND LEN(ProductCode) <= 9 AND ProductCode NOT LIKE '%.%' AND ProductCode NOT LIKE '%-%' AND ProductCode NOT LIKE '%+%'
                            THEN CAST(ProductCode AS BIGINT) 
                            ELSE 0 
                        END), 0) FROM Products");
                    if (resCode != null && resCode != DBNull.Value) nextSeq = Convert.ToInt64(resCode);
                }
                catch { }

                try
                {
                    var resId = DbHelper.ScalarTrans(trans, "SELECT COALESCE(MAX(ProductID), 0) FROM Products");
                    if (resId != null && resId != DBNull.Value)
                    {
                        long maxId = Convert.ToInt64(resId);
                        if (maxId > nextSeq) nextSeq = maxId;
                    }
                }
                catch { }

                if (nextSeq < 1000) nextSeq = 1000;

                foreach (var kvp in groups)
                {
                    var rows = kvp.Value;
                    if (rows.Count <= 1) continue;
                    string origCode = kvp.Key;

                    // ترتيب المجموعة تنازلياً: الأكثر حركات أولاً، ثم الأكثر رصيداً، ثم الأقدم ID
                    rows.Sort((a, b) =>
                    {
                        int tA = Convert.ToInt32(a["TotalTransactions"]);
                        int tB = Convert.ToInt32(b["TotalTransactions"]);
                        if (tA != tB) return tB.CompareTo(tA);

                        decimal sA = Math.Abs(Convert.ToDecimal(a["CurrentStock"]));
                        decimal sB = Math.Abs(Convert.ToDecimal(b["CurrentStock"]));
                        if (sA != sB) return sB.CompareTo(sA);

                        int idA = Convert.ToInt32(a["ProductID"]);
                        int idB = Convert.ToInt32(b["ProductID"]);
                        return idA.CompareTo(idB);
                    });

                    // الصنف الأول هو الصنف الأساسي (الأكثر حركات ورصيداً)
                    var primaryRow = rows[0];
                    int primaryID = Convert.ToInt32(primaryRow["ProductID"]);
                    string primaryName = primaryRow["ProductName"].ToString();
                    int pTrans = Convert.ToInt32(primaryRow["TotalTransactions"]);
                    decimal pStock = Convert.ToDecimal(primaryRow["CurrentStock"]);

                    fixLog.Add($"📌 كود الصنف [{origCode}]: تم الاحتفاظ به للصنف الأساسي [ID: {primaryID} - {primaryName}] (الأكثر نشاطاً: {pTrans} حركة، رصيد: {pStock:N2}).");

                    // الأصناف الأخرى المكررة (الأقل حركات)
                    for (int i = 1; i < rows.Count; i++)
                    {
                        var dupRow = rows[i];
                        int dupID = Convert.ToInt32(dupRow["ProductID"]);
                        string dupName = dupRow["ProductName"].ToString();
                        int dupTrans = Convert.ToInt32(dupRow["TotalTransactions"]);
                        decimal dupStock = Convert.ToDecimal(dupRow["CurrentStock"]);

                        // فحص شرط الحركات إذا كان التعديل مقصوراً على 0 حركات
                        if (onlyModifyZeroTransactions && dupTrans > 0)
                        {
                            fixLog.Add($"   🛡️ الصنف [ID: {dupID} - {dupName}]: تم تخطيه وحمايته من تعديل الكود لوجود حركات مسجلة ({dupTrans} حركة).");
                            continue;
                        }

                        // توليد الكود التسلسلي الجديد
                        string newCode;
                        if (long.TryParse(origCode, out _))
                        {
                            nextSeq++;
                            while (Convert.ToInt32(DbHelper.ScalarTrans(trans, "SELECT COUNT(1) FROM Products WHERE ProductCode = @c", DbHelper.P("@c", nextSeq.ToString()))) > 0)
                            {
                                nextSeq++;
                            }
                            newCode = nextSeq.ToString();
                        }
                        else
                        {
                            newCode = GenerateUniqueProductCodeTrans(trans, origCode, i);
                        }

                        // تحديث كود الصنف في جدول الأصناف وكافة الجداول المرتبطة
                        UpdateProductCodeEverywhereTrans(trans, dupID, newCode);

                        string transInfo = dupTrans > 0 ? $"مع تحديث كافة حركاته وتقاريره ({dupTrans} حركة، رصيد: {dupStock:N2})" : "بدون حركات سابقة";
                        fixLog.Add($"   ⚡ تم تعديل كود الصنف [ID: {dupID} - {dupName}] من [{origCode}] إلى الكود التسلسلي الجديد [{newCode}] ({transInfo})");
                        fixedCount++;
                    }
                }
            });

            ProductCache.Invalidate();
            return (fixedCount, fixLog);
        }

        private static void UpdateProductCodeEverywhereTrans(SqlTransaction trans, int productID, string newCode)
        {
            // 1. تحديث جدول الأصناف الرئيسي
            DbHelper.ExecuteTrans(trans,
                "UPDATE Products SET ProductCode = @nc WHERE ProductID = @id",
                DbHelper.P("@nc", newCode), DbHelper.P("@id", productID));

            // 2. تحديث جدول حجوزات العملاء إن وجد
            if (TableExistsTrans(trans, "CustomerReservationItems"))
            {
                DbHelper.ExecuteTrans(trans,
                    "UPDATE CustomerReservationItems SET ProductCode = @nc WHERE ProductID = @id",
                    DbHelper.P("@nc", newCode), DbHelper.P("@id", productID));
            }

            if (TableExistsTrans(trans, "CustomerReservations"))
            {
                DbHelper.ExecuteTrans(trans,
                    "UPDATE CustomerReservations SET ProductCode = @nc WHERE ProductID = @id",
                    DbHelper.P("@nc", newCode), DbHelper.P("@id", productID));
            }

            // 3. تحديث جدول كشكول النواقص إن وجد
            if (TableExistsTrans(trans, "ShortageNotebook"))
            {
                DbHelper.ExecuteTrans(trans,
                    "UPDATE ShortageNotebook SET ProductCode = @nc WHERE ProductID = @id",
                    DbHelper.P("@nc", newCode), DbHelper.P("@id", productID));
            }
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
                    // 1. تحويل كميات المخزون في ProductStock / WarehouseStock
                    string stockTable = TableExistsTrans(trans, "ProductStock") ? "ProductStock" : (TableExistsTrans(trans, "WarehouseStock") ? "WarehouseStock" : null);
                    if (stockTable != null)
                    {
                        var dtSourceStock = DbHelper.QueryTrans(trans,
                            $"SELECT WarehouseID, Quantity FROM {stockTable} WHERE ProductID = @pid",
                            DbHelper.P("@pid", sourceProductID));

                        foreach (DataRow sr in dtSourceStock.Rows)
                        {
                            int wid = Convert.ToInt32(sr["WarehouseID"]);
                            decimal qty = Convert.ToDecimal(sr["Quantity"]);

                            if (qty != 0)
                            {
                                var exists = DbHelper.ScalarTrans(trans,
                                    $"SELECT COUNT(*) FROM {stockTable} WHERE ProductID = @pid AND WarehouseID = @wid",
                                    DbHelper.P("@pid", targetProductID), DbHelper.P("@wid", wid));

                                if (exists != null && Convert.ToInt32(exists) > 0)
                                {
                                    DbHelper.ExecuteTrans(trans,
                                        $"UPDATE {stockTable} SET Quantity = Quantity + @q WHERE ProductID = @pid AND WarehouseID = @wid",
                                        DbHelper.P("@q", qty), DbHelper.P("@pid", targetProductID), DbHelper.P("@wid", wid));
                                }
                                else
                                {
                                    DbHelper.ExecuteTrans(trans,
                                        $"INSERT INTO {stockTable} (ProductID, WarehouseID, Quantity) VALUES (@pid, @wid, @q)",
                                        DbHelper.P("@pid", targetProductID), DbHelper.P("@wid", wid), DbHelper.P("@q", qty));
                                }
                            }
                        }
                        DbHelper.ExecuteTrans(trans, $"DELETE FROM {stockTable} WHERE ProductID = @pid", DbHelper.P("@pid", sourceProductID));
                    }

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

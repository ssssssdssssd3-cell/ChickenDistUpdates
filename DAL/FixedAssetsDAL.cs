using System;
using System.Collections.Generic;
using System.Data;
using ChickenDist.Core;

namespace ChickenDist.DAL
{
    public class DepreciationPreviewItem
    {
        public int AssetID { get; set; }
        public string AssetCode { get; set; }
        public string AssetName { get; set; }
        public string CategoryName { get; set; }
        public decimal PurchaseCost { get; set; }
        public decimal CurrentBookValue { get; set; }
        public decimal DepreciationRate { get; set; }
        public decimal MonthlyDepreciation { get; set; }
        public decimal BookValueAfter { get; set; }
        public decimal AccumulatedAfter { get; set; }
        public bool IsEligible { get; set; }
        public string Note { get; set; }
    }

    public static class FixedAssetsDAL
    {
        // ══════════════════════════════════════════════════
        // 1. التصنيفات (Categories)
        // ══════════════════════════════════════════════════
        public static DataTable GetAllCategories()
        {
            DbHelper.EnsureFixedAssetsAndShareholdersSchema();
            return DbHelper.Query("SELECT * FROM FixedAssetCategories ORDER BY CategoryID ASC");
        }

        public static int SaveCategory(int id, string name, decimal defaultRate, string method, string notes)
        {
            DbHelper.EnsureFixedAssetsAndShareholdersSchema();
            if (id > 0)
            {
                DbHelper.Execute(@"
                    UPDATE FixedAssetCategories 
                    SET CategoryName = @n, DefaultDepreciationRate = @r, DepreciationMethod = @m, Notes = @notes
                    WHERE CategoryID = @id",
                    DbHelper.P("@n", name), DbHelper.P("@r", defaultRate), DbHelper.P("@m", method), DbHelper.P("@notes", notes), DbHelper.P("@id", id));
                return id;
            }
            else
            {
                return DbHelper.ExecuteInsert(@"
                    INSERT INTO FixedAssetCategories (CategoryName, DefaultDepreciationRate, DepreciationMethod, Notes)
                    VALUES (@n, @r, @m, @notes)",
                    DbHelper.P("@n", name), DbHelper.P("@r", defaultRate), DbHelper.P("@m", method), DbHelper.P("@notes", notes));
            }
        }

        public static void DeleteCategory(int id)
        {
            var count = DbHelper.Scalar("SELECT COUNT(*) FROM FixedAssets WHERE CategoryID = @id", DbHelper.P("@id", id));
            if (count != null && Convert.ToInt32(count) > 0)
            {
                throw new Exception("لا يمكن حذف التصنيف لوجود أصول ثابتة تابعة له.");
            }
            DbHelper.Execute("DELETE FROM FixedAssetCategories WHERE CategoryID = @id", DbHelper.P("@id", id));
        }

        // ══════════════════════════════════════════════════
        // 2. إدارة الأصول (Fixed Assets CRUD)
        // ══════════════════════════════════════════════════
        public static string GenerateAssetCode()
        {
            DbHelper.EnsureFixedAssetsAndShareholdersSchema();
            string prefix = "AST-" + DateTime.Today.ToString("yyMM");
            var countObj = DbHelper.Scalar("SELECT COUNT(*) FROM FixedAssets WHERE AssetCode LIKE @p", DbHelper.P("@p", prefix + "%"));
            int count = countObj != null ? Convert.ToInt32(countObj) + 1 : 1;
            return $"{prefix}-{count:D3}";
        }

        public static DataTable GetAllAssets(string status = null, int categoryID = 0, string search = null)
        {
            DbHelper.EnsureFixedAssetsAndShareholdersSchema();
            string sql = @"
                SELECT fa.*, 
                       fac.CategoryName,
                       e.EmpName AS AssignedEmpName
                FROM FixedAssets fa
                LEFT JOIN FixedAssetCategories fac ON fa.CategoryID = fac.CategoryID
                LEFT JOIN Employees e ON fa.AssignedToEmpID = e.EmpID
                WHERE 1=1 ";

            var pars = new List<System.Data.SqlClient.SqlParameter>();

            if (!string.IsNullOrWhiteSpace(status) && status != "All")
            {
                sql += " AND fa.Status = @st";
                pars.Add(DbHelper.P("@st", status));
            }
            if (categoryID > 0)
            {
                sql += " AND fa.CategoryID = @cat";
                pars.Add(DbHelper.P("@cat", categoryID));
            }
            if (!string.IsNullOrWhiteSpace(search))
            {
                sql += " AND (fa.AssetName LIKE @q OR fa.AssetCode LIKE @q OR fa.Location LIKE @q)";
                pars.Add(DbHelper.P("@q", "%" + search.Trim() + "%"));
            }

            sql += " ORDER BY fa.AssetID DESC";
            return DbHelper.Query(sql, pars.ToArray());
        }

        public static DataRow GetAssetByID(int assetID)
        {
            DbHelper.EnsureFixedAssetsAndShareholdersSchema();
            var dt = DbHelper.Query(@"
                SELECT fa.*, 
                       fac.CategoryName,
                       e.EmpName AS AssignedEmpName
                FROM FixedAssets fa
                LEFT JOIN FixedAssetCategories fac ON fa.CategoryID = fac.CategoryID
                LEFT JOIN Employees e ON fa.AssignedToEmpID = e.EmpID
                WHERE fa.AssetID = @id", DbHelper.P("@id", assetID));
            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        public static int SaveAsset(int assetID, string code, string name, int? categoryID, DateTime purchaseDate,
            decimal purchaseCost, decimal salvageValue, int usefulLifeMonths, decimal depreciationRate,
            string depreciationMethod, string location, int? assignedEmpID, string status, string notes, int userID = 1)
        {
            DbHelper.EnsureFixedAssetsAndShareholdersSchema();
            if (string.IsNullOrWhiteSpace(name)) throw new Exception("يرجى إدخال اسم الأصل الثابت.");
            if (purchaseCost < 0) throw new Exception("يجب ألا تكون تكلفة الشراء سالبة.");

            if (string.IsNullOrWhiteSpace(code))
                code = GenerateAssetCode();

            if (assetID > 0)
            {
                DbHelper.Execute(@"
                    UPDATE FixedAssets
                    SET AssetCode = @code,
                        AssetName = @name,
                        CategoryID = @cat,
                        PurchaseDate = @pdate,
                        PurchaseCost = @cost,
                        SalvageValue = @salvage,
                        UsefulLifeMonths = @life,
                        DepreciationRate = @rate,
                        DepreciationMethod = @method,
                        Location = @loc,
                        AssignedToEmpID = @emp,
                        Status = @status,
                        Notes = @notes
                    WHERE AssetID = @id",
                    DbHelper.P("@code", code),
                    DbHelper.P("@name", name),
                    DbHelper.P("@cat", categoryID.HasValue ? (object)categoryID.Value : DBNull.Value),
                    DbHelper.P("@pdate", purchaseDate),
                    DbHelper.P("@cost", purchaseCost),
                    DbHelper.P("@salvage", salvageValue),
                    DbHelper.P("@life", usefulLifeMonths),
                    DbHelper.P("@rate", depreciationRate),
                    DbHelper.P("@method", depreciationMethod),
                    DbHelper.P("@loc", location),
                    DbHelper.P("@emp", assignedEmpID.HasValue && assignedEmpID.Value > 0 ? (object)assignedEmpID.Value : DBNull.Value),
                    DbHelper.P("@status", status ?? "Active"),
                    DbHelper.P("@notes", notes),
                    DbHelper.P("@id", assetID));

                return assetID;
            }
            else
            {
                decimal initialBookValue = purchaseCost;
                int id = DbHelper.ExecuteInsert(@"
                    INSERT INTO FixedAssets (AssetCode, AssetName, CategoryID, PurchaseDate, PurchaseCost, SalvageValue,
                                             UsefulLifeMonths, DepreciationRate, DepreciationMethod, Location, AssignedToEmpID,
                                             CurrentBookValue, TotalAccumulatedDepreciation, Status, Notes, CreatedBy, CreatedDate)
                    VALUES (@code, @name, @cat, @pdate, @cost, @salvage, @life, @rate, @method, @loc, @emp,
                            @bookVal, 0, @status, @notes, @uid, GETDATE())",
                    DbHelper.P("@code", code),
                    DbHelper.P("@name", name),
                    DbHelper.P("@cat", categoryID.HasValue ? (object)categoryID.Value : DBNull.Value),
                    DbHelper.P("@pdate", purchaseDate),
                    DbHelper.P("@cost", purchaseCost),
                    DbHelper.P("@salvage", salvageValue),
                    DbHelper.P("@life", usefulLifeMonths),
                    DbHelper.P("@rate", depreciationRate),
                    DbHelper.P("@method", depreciationMethod),
                    DbHelper.P("@loc", location),
                    DbHelper.P("@emp", assignedEmpID.HasValue && assignedEmpID.Value > 0 ? (object)assignedEmpID.Value : DBNull.Value),
                    DbHelper.P("@bookVal", initialBookValue),
                    DbHelper.P("@status", status ?? "Active"),
                    DbHelper.P("@notes", notes),
                    DbHelper.P("@uid", userID));

                // تسجيل حركة شراء أصل في العمليات
                RecordOperation(id, "Purchase", purchaseCost, $"تسجيل أصل ثابت جديد #{code} بتكلفة {purchaseCost:N2} ج", null, 0, userID);

                return id;
            }
        }

        public static void DeleteAsset(int assetID)
        {
            DbHelper.EnsureFixedAssetsAndShareholdersSchema();
            DbHelper.Execute("DELETE FROM FixedAssets WHERE AssetID = @id", DbHelper.P("@id", assetID));
        }

        // ══════════════════════════════════════════════════
        // 3. محرك احتساب الإهلاك الآلي (Depreciation Engine)
        // ══════════════════════════════════════════════════
        public static List<DepreciationPreviewItem> PreviewMonthlyDepreciation(string periodMonth, int? specificAssetID = null)
        {
            DbHelper.EnsureFixedAssetsAndShareholdersSchema();
            var list = new List<DepreciationPreviewItem>();

            string sql = @"
                SELECT fa.*, fac.CategoryName
                FROM FixedAssets fa
                LEFT JOIN FixedAssetCategories fac ON fa.CategoryID = fac.CategoryID
                WHERE fa.Status = 'Active' ";

            if (specificAssetID.HasValue && specificAssetID.Value > 0)
                sql += " AND fa.AssetID = " + specificAssetID.Value;

            DataTable dt = DbHelper.Query(sql);

            foreach (DataRow r in dt.Rows)
            {
                int assetID = Convert.ToInt32(r["AssetID"]);
                string code = r["AssetCode"].ToString();
                string name = r["AssetName"].ToString();
                string catName = r["CategoryName"] != DBNull.Value ? r["CategoryName"].ToString() : "عام";
                decimal cost = Convert.ToDecimal(r["PurchaseCost"]);
                decimal salvage = Convert.ToDecimal(r["SalvageValue"]);
                decimal currentBookVal = Convert.ToDecimal(r["CurrentBookValue"]);
                decimal accumulated = Convert.ToDecimal(r["TotalAccumulatedDepreciation"]);
                decimal rate = Convert.ToDecimal(r["DepreciationRate"]);
                string method = r["DepreciationMethod"].ToString();

                // فحص هل تم إهلاك هذا الشهر مسبقاً لهذا الأصل؟
                var alreadyDone = DbHelper.Scalar(
                    "SELECT COUNT(*) FROM FixedAssetDepreciations WHERE AssetID = @aid AND PeriodMonth = @pm",
                    DbHelper.P("@aid", assetID), DbHelper.P("@pm", periodMonth));

                bool isAlreadyPosted = alreadyDone != null && Convert.ToInt32(alreadyDone) > 0;

                decimal monthlyDep = 0m;
                bool eligible = true;
                string note = "";

                if (isAlreadyPosted)
                {
                    eligible = false;
                    note = "⚠️ تم قيد إهلاك هذا الشهر مسبقاً";
                }
                else if (rate <= 0)
                {
                    eligible = false;
                    note = "لا يخضع للإهلاك (نسبة 0%)";
                }
                else if (currentBookVal <= salvage)
                {
                    eligible = false;
                    note = "وصل الأصل إلى القيمة التخريدية/الدفترية الصفرية";
                }
                else
                {
                    if (method == "StraightLine")
                    {
                        // قسط ثابت: (التكلفة - الخردة) * (النسبة السنوية / 12)
                        decimal baseDepreciable = Math.Max(0m, cost - salvage);
                        monthlyDep = Math.Round(baseDepreciable * (rate / 100m) / 12m, 2);
                    }
                    else // ReducingBalance قسط متناقص
                    {
                        // (القيمة الدفترية الحالية) * (النسبة السنوية / 12)
                        monthlyDep = Math.Round(currentBookVal * (rate / 100m) / 12m, 2);
                    }

                    // لا يتجاوز القسط ما يجعل القيمة الدفترية أقل من الخردة
                    if (currentBookVal - monthlyDep < salvage)
                    {
                        monthlyDep = currentBookVal - salvage;
                    }

                    if (monthlyDep <= 0)
                    {
                        eligible = false;
                        note = "مكتمل الإهلاك";
                    }
                    else
                    {
                        note = "مستحق الإهلاك";
                    }
                }

                decimal bookValAfter = Math.Max(salvage, currentBookVal - monthlyDep);
                decimal accumAfter = accumulated + monthlyDep;

                list.Add(new DepreciationPreviewItem
                {
                    AssetID = assetID,
                    AssetCode = code,
                    AssetName = name,
                    CategoryName = catName,
                    PurchaseCost = cost,
                    CurrentBookValue = currentBookVal,
                    DepreciationRate = rate,
                    MonthlyDepreciation = monthlyDep,
                    BookValueAfter = bookValAfter,
                    AccumulatedAfter = accumAfter,
                    IsEligible = eligible,
                    Note = note
                });
            }

            return list;
        }

        public static int PostMonthlyDepreciation(string periodMonth, List<DepreciationPreviewItem> items, int userID = 1)
        {
            DbHelper.EnsureFixedAssetsAndShareholdersSchema();
            int postedCount = 0;

            DbHelper.RunInTransaction((con, trans) =>
            {
                foreach (var item in items)
                {
                    if (!item.IsEligible || item.MonthlyDepreciation <= 0) continue;

                    // 1. تسجيل حركة الإهلاك
                    DbHelper.ExecuteTrans(trans, @"
                        INSERT INTO FixedAssetDepreciations (AssetID, DepreciationDate, PeriodMonth, Amount, BookValueAfter, AccumulatedAfter, Notes, CreatedBy, CreatedDate)
                        VALUES (@aid, GETDATE(), @pm, @amt, @bva, @aca, @notes, @uid, GETDATE())",
                        DbHelper.P("@aid", item.AssetID),
                        DbHelper.P("@pm", periodMonth),
                        DbHelper.P("@amt", item.MonthlyDepreciation),
                        DbHelper.P("@bva", item.BookValueAfter),
                        DbHelper.P("@aca", item.AccumulatedAfter),
                        DbHelper.P("@notes", $"قسط إهلاك شهر {periodMonth}"),
                        DbHelper.P("@uid", userID));

                    // 2. تحديث الأصل
                    DbHelper.ExecuteTrans(trans, @"
                        UPDATE FixedAssets 
                        SET CurrentBookValue = @bva,
                            TotalAccumulatedDepreciation = @aca
                        WHERE AssetID = @aid",
                        DbHelper.P("@bva", item.BookValueAfter),
                        DbHelper.P("@aca", item.AccumulatedAfter),
                        DbHelper.P("@aid", item.AssetID));

                    // 3. قيد العملية في سجل العمليات
                    DbHelper.ExecuteTrans(trans, @"
                        INSERT INTO FixedAssetOperations (AssetID, OpType, OpDate, Amount, PaidFromSafeID, GainLossAmount, Notes, CreatedBy, CreatedDate)
                        VALUES (@aid, 'Depreciation', GETDATE(), @amt, NULL, 0, @notes, @uid, GETDATE())",
                        DbHelper.P("@aid", item.AssetID),
                        DbHelper.P("@amt", item.MonthlyDepreciation),
                        DbHelper.P("@notes", $"قسط إهلاك دوري لشهر {periodMonth}"),
                        DbHelper.P("@uid", userID));

                    postedCount++;
                }
            });

            return postedCount;
        }

        // ══════════════════════════════════════════════════
        // 4. العمليات الخاصة (صيانة، بيع، تخريد)
        // ══════════════════════════════════════════════════
        public static void RecordOperation(int assetID, string opType, decimal amount, string notes, int? safeID = null, decimal gainLoss = 0, int userID = 1)
        {
            DbHelper.EnsureFixedAssetsAndShareholdersSchema();
            DbHelper.Execute(@"
                INSERT INTO FixedAssetOperations (AssetID, OpType, OpDate, Amount, PaidFromSafeID, GainLossAmount, Notes, CreatedBy, CreatedDate)
                VALUES (@aid, @type, GETDATE(), @amt, @safe, @gl, @notes, @uid, GETDATE())",
                DbHelper.P("@aid", assetID),
                DbHelper.P("@type", opType),
                DbHelper.P("@amt", amount),
                DbHelper.P("@safe", safeID.HasValue && safeID.Value > 0 ? (object)safeID.Value : DBNull.Value),
                DbHelper.P("@gl", gainLoss),
                DbHelper.P("@notes", notes),
                DbHelper.P("@uid", userID));
        }

        public static void RecordMaintenance(int assetID, decimal cost, string notes, int? safeID, int userID = 1)
        {
            DbHelper.EnsureFixedAssetsAndShareholdersSchema();
            DbHelper.RunInTransaction((con, trans) =>
            {
                DbHelper.ExecuteTrans(trans, @"
                    INSERT INTO FixedAssetOperations (AssetID, OpType, OpDate, Amount, PaidFromSafeID, GainLossAmount, Notes, CreatedBy, CreatedDate)
                    VALUES (@aid, 'Maintenance', GETDATE(), @amt, @safe, 0, @notes, @uid, GETDATE())",
                    DbHelper.P("@aid", assetID),
                    DbHelper.P("@amt", cost),
                    DbHelper.P("@safe", safeID.HasValue && safeID.Value > 0 ? (object)safeID.Value : DBNull.Value),
                    DbHelper.P("@notes", notes),
                    DbHelper.P("@uid", userID));

                if (cost > 0 && safeID.HasValue && safeID.Value > 0)
                {
                    // خصم من الخزينة كمصروف صيانة
                    DbHelper.ExecuteTrans(trans, @"
                        INSERT INTO CashBox (TransType, AmountOut, RefID, Notes, CreatedBy, AccountID, TransDate)
                        VALUES ('Expense', @amt, @ref, @notes, @uid, @accId, GETDATE())",
                        DbHelper.P("@amt", cost),
                        DbHelper.P("@ref", assetID),
                        DbHelper.P("@notes", $"مصروف صيانة أصل ثابت: {notes}"),
                        DbHelper.P("@uid", userID),
                        DbHelper.P("@accId", safeID.Value));
                }
            });
        }

        public static void RecordSale(int assetID, decimal salePrice, string notes, int? safeID, int userID = 1)
        {
            DbHelper.EnsureFixedAssetsAndShareholdersSchema();
            var row = GetAssetByID(assetID);
            if (row == null) throw new Exception("الأصل غير موجود.");

            decimal bookValue = Convert.ToDecimal(row["CurrentBookValue"]);
            decimal gainLoss = salePrice - bookValue; // موجب = أرباح رأسمالية، سالب = خسائر رأسمالية

            DbHelper.RunInTransaction((con, trans) =>
            {
                // 1. تحديث حالة الأصل إلى Sold وقيمته الدفترية إلى 0
                DbHelper.ExecuteTrans(trans, @"
                    UPDATE FixedAssets 
                    SET Status = 'Sold', CurrentBookValue = 0, Notes = ISNULL(Notes, '') + @addNotes
                    WHERE AssetID = @aid",
                    DbHelper.P("@addNotes", $"\n[تم البيع بتاريخ {DateTime.Now:yyyy/MM/dd} بسعر {salePrice:N2} ج - صافي أرباح/خسائر: {gainLoss:N2} ج]"),
                    DbHelper.P("@aid", assetID));

                // 2. تسجيل حركة العملية
                DbHelper.ExecuteTrans(trans, @"
                    INSERT INTO FixedAssetOperations (AssetID, OpType, OpDate, Amount, PaidFromSafeID, GainLossAmount, Notes, CreatedBy, CreatedDate)
                    VALUES (@aid, 'Sale', GETDATE(), @amt, @safe, @gl, @notes, @uid, GETDATE())",
                    DbHelper.P("@aid", assetID),
                    DbHelper.P("@amt", salePrice),
                    DbHelper.P("@safe", safeID.HasValue && safeID.Value > 0 ? (object)safeID.Value : DBNull.Value),
                    DbHelper.P("@gl", gainLoss),
                    DbHelper.P("@notes", $"بيع أصل ثابت. القيمة الدفترية كانت: {bookValue:N2} ج. أرباح/خسائر: {gainLoss:N2} ج. {notes}"),
                    DbHelper.P("@uid", userID));

                // 3. توريد سعر البيع للخزينة
                if (salePrice > 0 && safeID.HasValue && safeID.Value > 0)
                {
                    DbHelper.ExecuteTrans(trans, @"
                        INSERT INTO CashBox (TransType, AmountIn, RefID, Notes, CreatedBy, AccountID, TransDate)
                        VALUES ('CapitalIncome', @amt, @ref, @notes, @uid, @accId, GETDATE())",
                        DbHelper.P("@amt", salePrice),
                        DbHelper.P("@ref", assetID),
                        DbHelper.P("@notes", $"إيراد بيع أصل ثابت #{row["AssetCode"]} - {row["AssetName"]}"),
                        DbHelper.P("@uid", userID),
                        DbHelper.P("@accId", safeID.Value));
                }
            });
        }

        public static void RecordScrap(int assetID, decimal scrapValue, string notes, int? safeID, int userID = 1)
        {
            DbHelper.EnsureFixedAssetsAndShareholdersSchema();
            var row = GetAssetByID(assetID);
            if (row == null) throw new Exception("الأصل غير موجود.");

            decimal bookValue = Convert.ToDecimal(row["CurrentBookValue"]);
            decimal loss = scrapValue - bookValue;

            DbHelper.RunInTransaction((con, trans) =>
            {
                DbHelper.ExecuteTrans(trans, @"
                    UPDATE FixedAssets 
                    SET Status = 'Scrapped', CurrentBookValue = 0, Notes = ISNULL(Notes, '') + @addNotes
                    WHERE AssetID = @aid",
                    DbHelper.P("@addNotes", $"\n[تم تخريد الأصل بتاريخ {DateTime.Now:yyyy/MM/dd} بقيمة خردة {scrapValue:N2} ج]"),
                    DbHelper.P("@aid", assetID));

                DbHelper.ExecuteTrans(trans, @"
                    INSERT INTO FixedAssetOperations (AssetID, OpType, OpDate, Amount, PaidFromSafeID, GainLossAmount, Notes, CreatedBy, CreatedDate)
                    VALUES (@aid, 'Scrap', GETDATE(), @amt, @safe, @gl, @notes, @uid, GETDATE())",
                    DbHelper.P("@aid", assetID),
                    DbHelper.P("@amt", scrapValue),
                    DbHelper.P("@safe", safeID.HasValue && safeID.Value > 0 ? (object)safeID.Value : DBNull.Value),
                    DbHelper.P("@gl", loss),
                    DbHelper.P("@notes", $"تخريد أصل ثابت. {notes}"),
                    DbHelper.P("@uid", userID));

                if (scrapValue > 0 && safeID.HasValue && safeID.Value > 0)
                {
                    DbHelper.ExecuteTrans(trans, @"
                        INSERT INTO CashBox (TransType, AmountIn, RefID, Notes, CreatedBy, AccountID, TransDate)
                        VALUES ('CapitalIncome', @amt, @ref, @notes, @uid, @accId, GETDATE())",
                        DbHelper.P("@amt", scrapValue),
                        DbHelper.P("@ref", assetID),
                        DbHelper.P("@notes", $"عائد تخريد أصل ثابت #{row["AssetCode"]}"),
                        DbHelper.P("@uid", userID),
                        DbHelper.P("@accId", safeID.Value));
                }
            });
        }

        // ══════════════════════════════════════════════════
        // 5. الإحصائيات وسجل الحركات
        // ══════════════════════════════════════════════════
        public static DataTable GetAssetOperations(int assetID)
        {
            DbHelper.EnsureFixedAssetsAndShareholdersSchema();
            return DbHelper.Query(@"
                SELECT fao.*, sa.AccountName AS SafeName, e.EmpName AS UserName
                FROM FixedAssetOperations fao
                LEFT JOIN SafeAccounts sa ON fao.PaidFromSafeID = sa.AccountID
                LEFT JOIN Employees e ON fao.CreatedBy = e.EmpID
                WHERE fao.AssetID = @aid
                ORDER BY fao.OpDate DESC", DbHelper.P("@aid", assetID));
        }

        public static DataTable GetAssetDepreciations(int assetID)
        {
            DbHelper.EnsureFixedAssetsAndShareholdersSchema();
            return DbHelper.Query(@"
                SELECT fad.*, e.EmpName AS UserName
                FROM FixedAssetDepreciations fad
                LEFT JOIN Employees e ON fad.CreatedBy = e.EmpID
                WHERE fad.AssetID = @aid
                ORDER BY fad.DepreciationDate DESC", DbHelper.P("@aid", assetID));
        }

        public static (decimal totalCost, decimal totalAccumulated, decimal totalBookValue, int activeCount) GetSummaryMetrics()
        {
            DbHelper.EnsureFixedAssetsAndShareholdersSchema();
            var dt = DbHelper.Query(@"
                SELECT 
                    ISNULL(SUM(PurchaseCost), 0) AS TotalCost,
                    ISNULL(SUM(TotalAccumulatedDepreciation), 0) AS TotalAccum,
                    ISNULL(SUM(CurrentBookValue), 0) AS TotalBookVal,
                    ISNULL(SUM(CASE WHEN Status = 'Active' THEN 1 ELSE 0 END), 0) AS ActiveCount
                FROM FixedAssets");

            if (dt.Rows.Count > 0)
            {
                var r = dt.Rows[0];
                return (
                    Convert.ToDecimal(r["TotalCost"]),
                    Convert.ToDecimal(r["TotalAccum"]),
                    Convert.ToDecimal(r["TotalBookVal"]),
                    Convert.ToInt32(r["ActiveCount"])
                );
            }

            return (0m, 0m, 0m, 0);
        }
    }
}

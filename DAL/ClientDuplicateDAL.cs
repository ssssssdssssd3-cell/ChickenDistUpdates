using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using ChickenDist.Core;

namespace ChickenDist.DAL
{
    /// <summary>
    /// فئة متخصصة لاكتشاف ومعالجة الأكواد المكررة للعملاء
    /// </summary>
    public static class ClientDuplicateDAL
    {
        public static DataTable GetDuplicateClientsReport()
        {
            string sql = @"
                WITH DupCodes AS (
                    SELECT ClientCode 
                    FROM Clients 
                    WHERE ClientCode IS NOT NULL AND LTRIM(RTRIM(ClientCode)) <> ''
                    GROUP BY ClientCode 
                    HAVING COUNT(*) > 1
                )
                SELECT c.ClientID, c.ClientCode, c.ClientName, c.Phone, c.Phone2, c.OpeningBalance, c.IsActive,
                       ISNULL((SELECT COUNT(*) FROM Sales s WHERE s.ClientID = c.ClientID), 0) AS SalesCount,
                       ISNULL((SELECT COUNT(*) FROM ClientTransactions ct WHERE ct.ClientID = c.ClientID), 0) AS TransCount,
                       ISNULL((SELECT COUNT(*) FROM Returns r WHERE r.ClientID = c.ClientID), 0) AS ReturnsCount,
                       (ISNULL((SELECT COUNT(*) FROM Sales s WHERE s.ClientID = c.ClientID), 0) + 
                        ISNULL((SELECT COUNT(*) FROM ClientTransactions ct WHERE ct.ClientID = c.ClientID), 0) +
                        ISNULL((SELECT COUNT(*) FROM Returns r WHERE r.ClientID = c.ClientID), 0)) AS TotalTransactions,
                       CASE 
                           WHEN (ISNULL((SELECT COUNT(*) FROM Sales s WHERE s.ClientID = c.ClientID), 0) + 
                                 ISNULL((SELECT COUNT(*) FROM ClientTransactions ct WHERE ct.ClientID = c.ClientID), 0) +
                                 ISNULL((SELECT COUNT(*) FROM Returns r WHERE r.ClientID = c.ClientID), 0)) > 0 
                                OR ABS(ISNULL(c.OpeningBalance, 0)) > 0.001 THEN 1 
                           ELSE 0 
                       END AS HasTransactions
                FROM Clients c
                WHERE c.ClientCode IN (SELECT ClientCode FROM DupCodes)
                ORDER BY c.ClientCode ASC, 
                         HasTransactions DESC, 
                         TotalTransactions DESC, 
                         c.ClientID ASC";
            return DbHelper.Query(sql);
        }

        public static (int totalFixed, List<string> fixLog) AutoFixDuplicateClientCodes(bool onlyModifyZeroTransactions = true)
        {
            DataTable dt = GetDuplicateClientsReport();
            var fixLog = new List<string>();
            int fixedCount = 0;

            if (dt == null || dt.Rows.Count == 0) return (0, fixLog);

            // تجميع العملاء حسب الكود المكرر
            var groups = new Dictionary<string, List<DataRow>>(StringComparer.OrdinalIgnoreCase);
            foreach (DataRow r in dt.Rows)
            {
                string code = r["ClientCode"]?.ToString().Trim() ?? "";
                if (string.IsNullOrEmpty(code)) continue;
                if (!groups.ContainsKey(code)) groups[code] = new List<DataRow>();
                groups[code].Add(r);
            }

            // حساب أعلى كود رقمي مستخدم حالياً
            int maxCode = 0;
            try
            {
                var resCode = DbHelper.Scalar(@"
                    SELECT COALESCE(MAX(CASE 
                        WHEN ISNUMERIC(ClientCode) = 1 AND LEN(ClientCode) <= 9 AND ClientCode NOT LIKE '%.%' AND ClientCode NOT LIKE '%-%' AND ClientCode NOT LIKE '%+%'
                        THEN CAST(ClientCode AS INT) 
                        ELSE 0 
                    END), 0) FROM Clients");
                if (resCode != null && resCode != DBNull.Value) maxCode = Convert.ToInt32(resCode);
            }
            catch { }

            try
            {
                var resId = DbHelper.Scalar("SELECT COALESCE(MAX(ClientID), 0) FROM Clients");
                if (resId != null && resId != DBNull.Value)
                {
                    int maxId = Convert.ToInt32(resId);
                    if (maxId > maxCode) maxCode = maxId;
                }
            }
            catch { }

            int nextCodeNum = maxCode + 1;

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

                    // الحالة 1: يوجد عميل له حركات مسجلة
                    if (withTrans.Count > 0)
                    {
                        var primary = withTrans[0];
                        int primaryID = Convert.ToInt32(primary["ClientID"]);
                        string primaryName = primary["ClientName"].ToString();
                        int pTrans = Convert.ToInt32(primary["TotalTransactions"]);

                        fixLog.Add($"📌 كود العميل [{origCode}]: تم الاحتفاظ به للعميل [ID: {primaryID} - {primaryName}] لوجود حركات مسجلة ({pTrans} حركة).");

                        // تعديل العملاء الذين ليس لهم حركات فقط
                        foreach (var dup in withoutTrans)
                        {
                            int dupID = Convert.ToInt32(dup["ClientID"]);
                            string dupName = dup["ClientName"].ToString();

                            string newCode = nextCodeNum.ToString();
                            while (Convert.ToInt32(DbHelper.ScalarTrans(trans, "SELECT COUNT(1) FROM Clients WHERE ClientCode = @c", DbHelper.P("@c", newCode))) > 0)
                            {
                                nextCodeNum++;
                                newCode = nextCodeNum.ToString();
                            }
                            nextCodeNum++;

                            DbHelper.ExecuteTrans(trans,
                                "UPDATE Clients SET ClientCode = @nc WHERE ClientID = @id",
                                DbHelper.P("@nc", newCode), DbHelper.P("@id", dupID));

                            fixLog.Add($"   ⚡ تم تعديل كود العميل [ID: {dupID} - {dupName}] (بدون أي حركات) من [{origCode}] إلى الكود الجديد الفريد [{newCode}]");
                            fixedCount++;
                        }

                        // إذا كان هناك عملاء آخرين لهم حركات أيضاً
                        if (withTrans.Count > 1)
                        {
                            for (int i = 1; i < withTrans.Count; i++)
                            {
                                var other = withTrans[i];
                                int oID = Convert.ToInt32(other["ClientID"]);
                                string oName = other["ClientName"].ToString();
                                int oTrans = Convert.ToInt32(other["TotalTransactions"]);

                                if (onlyModifyZeroTransactions)
                                {
                                    fixLog.Add($"   🛡️ العميل [ID: {oID} - {oName}]: تم تخطيه وحمايته من تعديل الكود لأن له حركات مسجلة ({oTrans} حركة).");
                                }
                                else
                                {
                                    string newCode = nextCodeNum.ToString();
                                    while (Convert.ToInt32(DbHelper.ScalarTrans(trans, "SELECT COUNT(1) FROM Clients WHERE ClientCode = @c", DbHelper.P("@c", newCode))) > 0)
                                    {
                                        nextCodeNum++;
                                        newCode = nextCodeNum.ToString();
                                    }
                                    nextCodeNum++;

                                    DbHelper.ExecuteTrans(trans,
                                        "UPDATE Clients SET ClientCode = @nc WHERE ClientID = @id",
                                        DbHelper.P("@nc", newCode), DbHelper.P("@id", oID));

                                    fixLog.Add($"   ⚡ تم تعديل كود العميل [ID: {oID} - {oName}] من [{origCode}] إلى [{newCode}]");
                                    fixedCount++;
                                }
                            }
                        }
                    }
                    else
                    {
                        // الحالة 2: جميع العملاء المشتركين في الكود ليس لهم حركات على الإطلاق (0 حركات)
                        var primary = withoutTrans[0];
                        int primaryID = Convert.ToInt32(primary["ClientID"]);
                        string primaryName = primary["ClientName"].ToString();

                        fixLog.Add($"📌 كود العميل [{origCode}]: تم الاحتفاظ به للعميل الأقدم [ID: {primaryID} - {primaryName}] (بدون حركات).");

                        for (int i = 1; i < withoutTrans.Count; i++)
                        {
                            var dup = withoutTrans[i];
                            int dupID = Convert.ToInt32(dup["ClientID"]);
                            string dupName = dup["ClientName"].ToString();

                            string newCode = nextCodeNum.ToString();
                            while (Convert.ToInt32(DbHelper.ScalarTrans(trans, "SELECT COUNT(1) FROM Clients WHERE ClientCode = @c", DbHelper.P("@c", newCode))) > 0)
                            {
                                nextCodeNum++;
                                newCode = nextCodeNum.ToString();
                            }
                            nextCodeNum++;

                            DbHelper.ExecuteTrans(trans,
                                "UPDATE Clients SET ClientCode = @nc WHERE ClientID = @id",
                                DbHelper.P("@nc", newCode), DbHelper.P("@id", dupID));

                            fixLog.Add($"   ⚡ تم تعديل كود العميل [ID: {dupID} - {dupName}] (بدون أي حركات) من [{origCode}] إلى الكود الجديد الفريد [{newCode}]");
                            fixedCount++;
                        }
                    }
                }
            });

            ClientCache.Invalidate();
            return (fixedCount, fixLog);
        }
    }
}

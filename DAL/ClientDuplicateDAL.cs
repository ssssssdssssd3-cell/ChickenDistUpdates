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
                       ISNULL((SELECT COUNT(*) FROM ClientTransactions ct WHERE ct.ClientID = c.ClientID), 0) AS TransCount
                FROM Clients c
                WHERE c.ClientCode IN (SELECT ClientCode FROM DupCodes)
                ORDER BY c.ClientCode ASC, (ISNULL((SELECT COUNT(*) FROM Sales s WHERE s.ClientID = c.ClientID), 0) + ISNULL((SELECT COUNT(*) FROM ClientTransactions ct WHERE ct.ClientID = c.ClientID), 0)) DESC, c.ClientID ASC";
            return DbHelper.Query(sql);
        }

        public static (int totalFixed, List<string> fixLog) AutoFixDuplicateClientCodes()
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

                    var primary = rows[0];
                    int primaryID = Convert.ToInt32(primary["ClientID"]);
                    string primaryName = primary["ClientName"].ToString();
                    string origCode = kvp.Key;

                    fixLog.Add($"📌 كود العميل [{origCode}]: تم الاحتفاظ به للعميل الأساسي [ID: {primaryID} - {primaryName}]");

                    for (int i = 1; i < rows.Count; i++)
                    {
                        var dup = rows[i];
                        int dupID = Convert.ToInt32(dup["ClientID"]);
                        string dupName = dup["ClientName"].ToString();

                        // البحث عن كود فريد غير مستخدم
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

                        fixLog.Add($"   ⚡ تم تعديل كود العميل [ID: {dupID} - {dupName}] من [{origCode}] إلى الكود الفريد الجديد [{newCode}]");
                        fixedCount++;
                    }
                }
            });

            ClientCache.Invalidate();
            return (fixedCount, fixLog);
        }
    }
}

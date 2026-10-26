using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using ChickenDist.Core;

namespace ChickenDist.DAL
{
    public class JournalLine
    {
        public string AccountName { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }

        public JournalLine() { }
        public JournalLine(string accountName, decimal debit, decimal credit)
        {
            AccountName = accountName;
            Debit = debit;
            Credit = credit;
        }
    }

    /// <summary>
    /// طبقة إدارة قيود اليومية العامة المزدوجة (Double-Entry General Ledger)
    /// تضمن الترحيل اللحظي المتوازن (Debit = Credit) والربط الذري مع العمليات المالية
    /// </summary>
    public static class JournalDAL
    {
        /// <summary>
        /// ترحيل قيد يومية محاسبي مزدوج ذري داخل SqlTransaction
        /// </summary>
        public static int PostEntryTrans(SqlTransaction trans, DateTime date, string description, string sourceType, int sourceRefId, int? createdBy, IEnumerable<JournalLine> lines)
        {
            if (trans == null) throw new ArgumentNullException(nameof(trans));
            if (lines == null) return -1;

            var validLines = lines.Where(l => l != null && (l.Debit > 0m || l.Credit > 0m)).ToList();
            if (validLines.Count == 0)
            {
                // إذا لم تكن هناك بنود مالية موجبة، نقوم بحذف أي قيد سابق ونعود
                DeleteEntryTrans(trans, sourceType, sourceRefId);
                return 0;
            }

            decimal totalDebit = Math.Round(validLines.Sum(l => l.Debit), 2);
            decimal totalCredit = Math.Round(validLines.Sum(l => l.Credit), 2);

            // التحقق من توازن القيد المحاسبي المزدوج
            decimal diff = Math.Round(totalDebit - totalCredit, 2);
            if (Math.Abs(diff) > 0.05m)
            {
                throw new InvalidOperationException($"القيد المحاسبي غير متوازن! إجمالي المدين: {totalDebit:N2} ج، إجمالي الدائن: {totalCredit:N2} ج، الفارق: {diff:N2} ج.");
            }
            else if (diff != 0m)
            {
                // تسوية فروق التقريب الطفيفة (أقل من أو يساوي 5 قروش)
                var lastLine = validLines.Last();
                if (diff > 0m)
                {
                    // المدين أكبر بقليل -> زيادة الدائن في السطر الأخير
                    lastLine.Credit += diff;
                }
                else
                {
                    // الدائن أكبر بقليل -> زيادة المدين في السطر الأخير
                    lastLine.Debit += Math.Abs(diff);
                }
            }

            // حذف أي قيد سابق لنفس المصدر منعاً للتكرار (ON DELETE CASCADE يحذف JournalDetails تلقائياً)
            DeleteEntryTrans(trans, sourceType, sourceRefId);

            int entryId = DbHelper.ExecuteInsertTrans(trans,
                @"INSERT INTO JournalEntries (EntryDate, Description, SourceType, SourceRefID, CreatedBy)
                  VALUES (@dt, @desc, @st, @ref, @by)",
                DbHelper.P("@dt", date),
                DbHelper.P("@desc", string.IsNullOrEmpty(description) ? (object)DBNull.Value : (description.Length > 490 ? description.Substring(0, 490) : description)),
                DbHelper.P("@st", sourceType),
                DbHelper.P("@ref", sourceRefId),
                DbHelper.P("@by", createdBy.HasValue && createdBy.Value > 0 ? (object)createdBy.Value : DBNull.Value));

            if (entryId <= 0)
                throw new Exception("فشل في إنشاء رأس القيد المحاسبي JournalEntries.");

            foreach (var line in validLines)
            {
                DbHelper.ExecuteTrans(trans,
                    @"INSERT INTO JournalDetails (EntryID, AccountName, Debit, Credit)
                      VALUES (@eid, @acc, @dr, @cr)",
                    DbHelper.P("@eid", entryId),
                    DbHelper.P("@acc", string.IsNullOrEmpty(line.AccountName) ? "حساب عام" : (line.AccountName.Length > 490 ? line.AccountName.Substring(0, 490) : line.AccountName)),
                    DbHelper.P("@dr", Math.Round(line.Debit, 2)),
                    DbHelper.P("@cr", Math.Round(line.Credit, 2)));
            }

            return entryId;
        }

        /// <summary>
        /// حذف القيد المحاسبي المرتبط بمصدر معين داخل Transaction
        /// </summary>
        public static void DeleteEntryTrans(SqlTransaction trans, string sourceType, int sourceRefId)
        {
            DbHelper.ExecuteTrans(trans,
                "DELETE FROM JournalEntries WHERE SourceType = @st AND SourceRefID = @ref",
                DbHelper.P("@st", sourceType),
                DbHelper.P("@ref", sourceRefId));
        }

        /// <summary>
        /// ترحيل قيد فاتورة مبيعات
        /// </summary>
        public static void PostSaleJournalTrans(SqlTransaction trans, int saleID, string saleCode, DateTime saleDate, string saleType, decimal totalAmount, int? clientID, string clientName, decimal? cashPaid, decimal? visaPaid, int? safeAccountID, int? createdBy)
        {
            if (totalAmount <= 0m) return;

            string safeName = GetSafeAccountNameTrans(trans, safeAccountID ?? (Session.DefaultSafeID ?? Session.GetDefaultSafeID()), "حـ/ الخزينة الرئيسية");
            string visaName = GetSafeAccountNameTrans(trans, safeAccountID, "حـ/ البنك - الفيزا");
            string clientAcc = !string.IsNullOrWhiteSpace(clientName) ? $"حـ/ العميل: {clientName.Trim()}" : (clientID.HasValue ? $"حـ/ العميل رقم {clientID.Value}" : "حـ/ العملاء - بيع آجل");

            var lines = new List<JournalLine>();

            // الطرف الدائن: حساب المبيعات
            lines.Add(new JournalLine("حساب المبيعات", 0m, totalAmount));

            // الطرف المدين: بحسب طريقة الدفع
            if (saleType == "Cash")
            {
                lines.Add(new JournalLine(safeName, totalAmount, 0m));
            }
            else if (saleType == "Credit" || saleType == "Installment")
            {
                lines.Add(new JournalLine(clientAcc, totalAmount, 0m));
            }
            else if (saleType == "Visa")
            {
                lines.Add(new JournalLine(visaName, totalAmount, 0m));
            }
            else if (saleType == "Mixed")
            {
                decimal cp = cashPaid ?? 0m;
                decimal vp = visaPaid ?? 0m;
                decimal remaining = totalAmount - cp - vp;

                if (cp > 0m) lines.Add(new JournalLine(safeName, cp, 0m));
                if (vp > 0m) lines.Add(new JournalLine(visaName, vp, 0m));
                if (remaining > 0m) lines.Add(new JournalLine(clientAcc, remaining, 0m));
            }
            else if (saleType == "DriverLoad")
            {
                lines.Add(new JournalLine("حـ/ عهدة المندوب", totalAmount, 0m));
            }
            else
            {
                lines.Add(new JournalLine(safeName, totalAmount, 0m));
            }

            string desc = $"فاتورة مبيعات رقم {saleCode}";
            PostEntryTrans(trans, saleDate, desc, "Sale", saleID, createdBy, lines);
        }

        /// <summary>
        /// ترحيل قيد مرتجع مبيعات
        /// </summary>
        public static void PostSaleReturnJournalTrans(SqlTransaction trans, int returnID, int? saleID, DateTime returnDate, string paymentType, decimal totalAmount, int? clientID, string clientName, int? safeAccountID, int? createdBy)
        {
            if (totalAmount <= 0m) return;

            string safeName = GetSafeAccountNameTrans(trans, safeAccountID ?? (Session.DefaultSafeID ?? Session.GetDefaultSafeID()), "حـ/ الخزينة الرئيسية");
            string visaName = GetSafeAccountNameTrans(trans, safeAccountID, "حـ/ البنك - الفيزا");
            string clientAcc = !string.IsNullOrWhiteSpace(clientName) ? $"حـ/ العميل: {clientName.Trim()}" : (clientID.HasValue ? $"حـ/ العميل رقم {clientID.Value}" : "حـ/ العملاء - مرتجع آجل");

            var lines = new List<JournalLine>();

            // الطرف المدين: مردودات المبيعات
            lines.Add(new JournalLine("حـ/ مردودات المبيعات", totalAmount, 0m));

            // الطرف الدائن: بحسب طريقة الصرف
            if (paymentType == "Cash")
            {
                lines.Add(new JournalLine(safeName, 0m, totalAmount));
            }
            else if (paymentType == "Visa")
            {
                lines.Add(new JournalLine(visaName, 0m, totalAmount));
            }
            else // Credit or other
            {
                lines.Add(new JournalLine(clientAcc, 0m, totalAmount));
            }

            string desc = saleID.HasValue && saleID.Value > 0
                ? $"مرتجع فاتورة مبيعات رقم {saleID.Value}"
                : $"مرتجع مبيعات عام رقم {returnID}";

            PostEntryTrans(trans, returnDate, desc, "SaleReturn", returnID, createdBy, lines);
        }

        /// <summary>
        /// ترحيل قيد فاتورة مشتريات
        /// </summary>
        public static void PostPurchaseJournalTrans(SqlTransaction trans, int purchaseID, string purchaseCode, DateTime purchaseDate, string purchaseType, decimal totalAmount, int? supplierID, string supplierName, int? safeAccountID, int? createdBy)
        {
            if (totalAmount <= 0m) return;

            string safeName = GetSafeAccountNameTrans(trans, safeAccountID ?? (Session.DefaultSafeID ?? Session.GetDefaultSafeID()), "حـ/ الخزينة الرئيسية");
            string suppAcc = !string.IsNullOrWhiteSpace(supplierName) ? $"حـ/ المورد: {supplierName.Trim()}" : (supplierID.HasValue ? $"حـ/ المورد رقم {supplierID.Value}" : "حـ/ الموردين - شراء آجل");

            var lines = new List<JournalLine>();

            // الطرف المدين: حساب المشتريات
            lines.Add(new JournalLine("حساب المشتريات", totalAmount, 0m));

            // الطرف الدائن: بحسب نوع الشراء
            if (purchaseType == "Cash")
            {
                lines.Add(new JournalLine(safeName, 0m, totalAmount));
            }
            else // Credit
            {
                lines.Add(new JournalLine(suppAcc, 0m, totalAmount));
            }

            string desc = $"فاتورة مشتريات رقم {purchaseCode}";
            PostEntryTrans(trans, purchaseDate, desc, "Purchase", purchaseID, createdBy, lines);
        }

        /// <summary>
        /// ترحيل قيد مرتجع مشتريات
        /// </summary>
        public static void PostPurchaseReturnJournalTrans(SqlTransaction trans, int returnID, int? purchaseID, DateTime returnDate, string returnType, decimal totalAmount, int? supplierID, string supplierName, int? safeAccountID, int? createdBy)
        {
            if (totalAmount <= 0m) return;

            string safeName = GetSafeAccountNameTrans(trans, safeAccountID ?? (Session.DefaultSafeID ?? Session.GetDefaultSafeID()), "حـ/ الخزينة الرئيسية");
            string suppAcc = !string.IsNullOrWhiteSpace(supplierName) ? $"حـ/ المورد: {supplierName.Trim()}" : (supplierID.HasValue ? $"حـ/ المورد رقم {supplierID.Value}" : "حـ/ الموردين - مرتجع شراء آجل");

            var lines = new List<JournalLine>();

            // الطرف الدائن: مردودات المشتريات
            lines.Add(new JournalLine("حـ/ مردودات المشتريات", 0m, totalAmount));

            // الطرف المدين: بحسب طريقة الاسترداد
            if (returnType == "Cash")
            {
                lines.Add(new JournalLine(safeName, totalAmount, 0m));
            }
            else // Credit
            {
                lines.Add(new JournalLine(suppAcc, totalAmount, 0m));
            }

            string desc = purchaseID.HasValue && purchaseID.Value > 0
                ? $"مرتجع فاتورة مشتريات رقم {purchaseID.Value}"
                : $"مرتجع مشتريات عام رقم {returnID}";

            PostEntryTrans(trans, returnDate, desc, "PurchaseReturn", returnID, createdBy, lines);
        }

        private static string GetSafeAccountNameTrans(SqlTransaction trans, int? accountID, string defaultName)
        {
            if (!accountID.HasValue || accountID.Value <= 0) return defaultName;
            try
            {
                var val = DbHelper.ScalarTrans(trans, "SELECT AccountName FROM SafeAccounts WHERE AccountID = @id", DbHelper.P("@id", accountID.Value));
                if (val != null && val != DBNull.Value)
                {
                    string name = val.ToString().Trim();
                    if (!string.IsNullOrEmpty(name)) return name;
                }
            }
            catch { }
            return defaultName;
        }

        /// <summary>
        /// جلب سجل قيود اليومية العامة للاستعراض في شاشة الحسابات
        /// </summary>
        public static DataTable GetJournalEntriesHistory(DateTime fromDate, DateTime toDate, string sourceType = null)
        {
            string sql = @"
                SELECT 
                    je.EntryID AS [رقم القيد],
                    je.EntryDate AS [التاريخ],
                    je.Description AS [البيان العام],
                    je.SourceType AS [المصدر],
                    je.SourceRefID AS [رقم المرجع],
                    ISNULL(SUM(jd.Debit), 0) AS [إجمالي المدين],
                    ISNULL(SUM(jd.Credit), 0) AS [إجمالي الدائن],
                    CASE 
                        WHEN ABS(ISNULL(SUM(jd.Debit), 0) - ISNULL(SUM(jd.Credit), 0)) < 0.01 THEN N'متوازن'
                        ELSE N'غير متوازن'
                    END AS [الحالة]
                FROM JournalEntries je
                LEFT JOIN JournalDetails jd ON je.EntryID = jd.EntryID
                WHERE je.EntryDate BETWEEN @f AND @t
                  AND (@st IS NULL OR je.SourceType = @st)
                GROUP BY je.EntryID, je.EntryDate, je.Description, je.SourceType, je.SourceRefID
                ORDER BY je.EntryDate DESC, je.EntryID DESC";

            return DbHelper.Query(sql,
                DbHelper.P("@f", fromDate),
                DbHelper.P("@t", toDate),
                DbHelper.P("@st", string.IsNullOrEmpty(sourceType) ? (object)DBNull.Value : sourceType));
        }

        /// <summary>
        /// جلب أسطر وتفاصيل قيد محدد
        /// </summary>
        public static DataTable GetJournalDetails(int entryId)
        {
            string sql = @"
                SELECT 
                    DetailID,
                    AccountName AS [اسم الحساب],
                    Debit AS [مدين],
                    Credit AS [دائن]
                FROM JournalDetails
                WHERE EntryID = @eid
                ORDER BY DetailID ASC";

            return DbHelper.Query(sql, DbHelper.P("@eid", entryId));
        }
    }
}

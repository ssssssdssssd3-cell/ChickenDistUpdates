using System;
using System.Collections.Generic;
using System.Data;
using ChickenDist.Core;

namespace ChickenDist.DAL
{
    public class DividendDistributionLineDTO
    {
        public int LineID { get; set; }
        public int PartnerID { get; set; }
        public string PartnerName { get; set; }
        public decimal SharePercentage { get; set; }
        public decimal CalculatedProfit { get; set; }
        public bool IsPaid { get; set; }
        public decimal PaidAmount { get; set; }
        public DateTime? PaidDate { get; set; }
        public int? PaidSafeID { get; set; }
    }

    public static class ShareholdersDAL
    {
        // ══════════════════════════════════════════════════
        // 1. إدارة الشركاء والمساهمين (Partners CRUD)
        // ══════════════════════════════════════════════════
        public static string GeneratePartnerCode()
        {
            DbHelper.EnsureFixedAssetsAndShareholdersSchema();
            string prefix = "PRT-" + DateTime.Today.ToString("yyMM");
            var countObj = DbHelper.Scalar("SELECT COUNT(*) FROM Partners WHERE PartnerCode LIKE @p", DbHelper.P("@p", prefix + "%"));
            int count = countObj != null ? Convert.ToInt32(countObj) + 1 : 1;
            return $"{prefix}-{count:D3}";
        }

        public static decimal GetTotalSharePercentage(int excludePartnerID = 0)
        {
            DbHelper.EnsureFixedAssetsAndShareholdersSchema();
            var res = DbHelper.Scalar(@"
                SELECT ISNULL(SUM(SharePercentage), 0) 
                FROM Partners 
                WHERE IsActive = 1 AND PartnerID <> @id", DbHelper.P("@id", excludePartnerID));
            return res != null && res != DBNull.Value ? Convert.ToDecimal(res) : 0m;
        }

        public static DataTable GetAllPartners(bool activeOnly = false)
        {
            DbHelper.EnsureFixedAssetsAndShareholdersSchema();
            string sql = "SELECT * FROM Partners ";
            if (activeOnly) sql += " WHERE IsActive = 1 ";
            sql += " ORDER BY SharePercentage DESC, PartnerID ASC";
            return DbHelper.Query(sql);
        }

        public static DataRow GetPartnerByID(int partnerID)
        {
            DbHelper.EnsureFixedAssetsAndShareholdersSchema();
            var dt = DbHelper.Query("SELECT * FROM Partners WHERE PartnerID = @id", DbHelper.P("@id", partnerID));
            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        public static int SavePartner(int partnerID, string code, string name, string phone, string nationalID,
            decimal sharePct, decimal capitalContribution, bool isActive, string notes, int userID = 1, int? initialSafeID = null)
        {
            DbHelper.EnsureFixedAssetsAndShareholdersSchema();
            if (string.IsNullOrWhiteSpace(name)) throw new Exception("يرجى إدخال اسم الشريك/المساهم.");

            decimal currentTotal = GetTotalSharePercentage(partnerID);
            if (currentTotal + sharePct > 100.001m)
            {
                throw new Exception($"مجموع نسب الشركاء لا يمكن أن يتجاوز 100% (المتاح حالياً: {100m - currentTotal:F2}%).");
            }

            if (string.IsNullOrWhiteSpace(code))
                code = GeneratePartnerCode();

            int id = partnerID;

            DbHelper.RunInTransaction((con, trans) =>
            {
                if (partnerID > 0)
                {
                    // جلب رأس المال القديم لاحتساب الفرق والتأثير على الخزينة
                    var oldCapObj = DbHelper.ScalarTrans(trans, "SELECT CapitalContribution FROM Partners WHERE PartnerID = @id", DbHelper.P("@id", partnerID));
                    decimal oldCap = (oldCapObj != null && oldCapObj != DBNull.Value) ? Convert.ToDecimal(oldCapObj) : 0m;
                    decimal diff = capitalContribution - oldCap;
                    int sId = (initialSafeID.HasValue && initialSafeID.Value > 0) ? initialSafeID.Value : 1;

                    DbHelper.ExecuteTrans(trans, @"
                        UPDATE Partners
                        SET PartnerCode = @code,
                            PartnerName = @name,
                            Phone = @phone,
                            NationalID = @nid,
                            SharePercentage = @pct,
                            CapitalContribution = @cap,
                            IsActive = @act,
                            Notes = @notes
                        WHERE PartnerID = @id",
                        DbHelper.P("@code", code),
                        DbHelper.P("@name", name),
                        DbHelper.P("@phone", phone),
                        DbHelper.P("@nid", nationalID),
                        DbHelper.P("@pct", sharePct),
                        DbHelper.P("@cap", capitalContribution),
                        DbHelper.P("@act", isActive),
                        DbHelper.P("@notes", notes),
                        DbHelper.P("@id", partnerID));

                    // إذا تم تعديل رأس المال بالزيادة أو النقصان ➔ تأثير فوري على الخزينة وحساب الشريك
                    if (diff > 0)
                    {
                        // 1. زيادة رأس المال -> توريد نقدية إلى الخزينة
                        DbHelper.ExecuteTrans(trans, @"
                            INSERT INTO CashBox (TransType, AmountIn, RefID, Notes, CreatedBy, AccountID, TransDate)
                            VALUES ('CapitalDeposit', @amt, @ref, @notes, @uid, @accId, GETDATE())",
                            DbHelper.P("@amt", diff),
                            DbHelper.P("@ref", partnerID),
                            DbHelper.P("@notes", $"توريد زيادة رأس مال للشريك [{name}] بمبلغ {diff:N2} ج"),
                            DbHelper.P("@uid", userID),
                            DbHelper.P("@accId", sId));

                        // 2. قيد دائن للشريك بفرق الزيادة
                        DbHelper.ExecuteTrans(trans, @"
                            INSERT INTO PartnerTransactions (PartnerID, TransDate, TransType, Debit, Credit, Notes, SafeID, CreatedBy, CreatedDate)
                            VALUES (@pid, GETDATE(), 'CapitalDeposit', 0, @amt, @notes, @safe, @uid, GETDATE())",
                            DbHelper.P("@pid", partnerID),
                            DbHelper.P("@amt", diff),
                            DbHelper.P("@notes", $"زيادة رأس مال الشريك بمبلغ {diff:N2} ج"),
                            DbHelper.P("@safe", sId),
                            DbHelper.P("@uid", userID));

                        // 3. زيادة الرصيد الجاري
                        DbHelper.ExecuteTrans(trans, @"
                            UPDATE Partners
                            SET CurrentBalance = CurrentBalance + @amt
                            WHERE PartnerID = @id",
                            DbHelper.P("@amt", diff),
                            DbHelper.P("@id", partnerID));
                    }
                    else if (diff < 0)
                    {
                        decimal refundAmt = Math.Abs(diff);
                        // فحص كفاية رصيد الخزينة
                        AccountDAL.EnsureSufficientCashTrans(trans, sId, refundAmt, $"تخفيض واسترداد رأس مال للشريك [{name}]");

                        // 1. تخفيض رأس المال -> صرف نقدية من الخزينة
                        DbHelper.ExecuteTrans(trans, @"
                            INSERT INTO CashBox (TransType, AmountOut, RefID, Notes, CreatedBy, AccountID, TransDate)
                            VALUES ('CapitalWithdrawal', @amt, @ref, @notes, @uid, @accId, GETDATE())",
                            DbHelper.P("@amt", refundAmt),
                            DbHelper.P("@ref", partnerID),
                            DbHelper.P("@notes", $"صرف تخفيض رأس مال للشريك [{name}] بمبلغ {refundAmt:N2} ج"),
                            DbHelper.P("@uid", userID),
                            DbHelper.P("@accId", sId));

                        // 2. قيد مدين للشريك بفرق التخفيض
                        DbHelper.ExecuteTrans(trans, @"
                            INSERT INTO PartnerTransactions (PartnerID, TransDate, TransType, Debit, Credit, Notes, SafeID, CreatedBy, CreatedDate)
                            VALUES (@pid, GETDATE(), 'CapitalWithdrawal', @amt, 0, @notes, @safe, @uid, GETDATE())",
                            DbHelper.P("@pid", partnerID),
                            DbHelper.P("@amt", refundAmt),
                            DbHelper.P("@notes", $"تخفيض رأس مال الشريك بمبلغ {refundAmt:N2} ج"),
                            DbHelper.P("@safe", sId),
                            DbHelper.P("@uid", userID));

                        // 3. خصم من الرصيد الجاري
                        DbHelper.ExecuteTrans(trans, @"
                            UPDATE Partners
                            SET CurrentBalance = CurrentBalance - @amt
                            WHERE PartnerID = @id",
                            DbHelper.P("@amt", refundAmt),
                            DbHelper.P("@id", partnerID));
                    }
                }
                else
                {
                    int sId = (initialSafeID.HasValue && initialSafeID.Value > 0) ? initialSafeID.Value : 1;

                    id = DbHelper.ExecuteInsertTrans(trans, @"
                        INSERT INTO Partners (PartnerCode, PartnerName, Phone, NationalID, SharePercentage,
                                              CapitalContribution, CurrentBalance, JoinDate, IsActive, Notes, CreatedBy, CreatedDate)
                        VALUES (@code, @name, @phone, @nid, @pct, @cap, @cap, GETDATE(), @act, @notes, @uid, GETDATE())",
                        DbHelper.P("@code", code),
                        DbHelper.P("@name", name),
                        DbHelper.P("@phone", phone),
                        DbHelper.P("@nid", nationalID),
                        DbHelper.P("@pct", sharePct),
                        DbHelper.P("@cap", capitalContribution),
                        DbHelper.P("@act", isActive),
                        DbHelper.P("@notes", notes),
                        DbHelper.P("@uid", userID));

                    // إذا تم إيداع رأس مال أولي
                    if (capitalContribution > 0)
                    {
                        DbHelper.ExecuteTrans(trans, @"
                            INSERT INTO PartnerTransactions (PartnerID, TransDate, TransType, Debit, Credit, Notes, SafeID, CreatedBy, CreatedDate)
                            VALUES (@pid, GETDATE(), 'CapitalDeposit', 0, @amt, @notes, @safe, @uid, GETDATE())",
                            DbHelper.P("@pid", id),
                            DbHelper.P("@amt", capitalContribution),
                            DbHelper.P("@notes", $"إيداع رأس مال مبدئي - حصة {sharePct}%"),
                            DbHelper.P("@safe", sId),
                            DbHelper.P("@uid", userID));

                        DbHelper.ExecuteTrans(trans, @"
                            INSERT INTO CashBox (TransType, AmountIn, RefID, Notes, CreatedBy, AccountID, TransDate)
                            VALUES ('CapitalDeposit', @amt, @ref, @notes, @uid, @accId, GETDATE())",
                            DbHelper.P("@amt", capitalContribution),
                            DbHelper.P("@ref", id),
                            DbHelper.P("@notes", $"إيداع رأس مال للشريك [{name}]"),
                            DbHelper.P("@uid", userID),
                            DbHelper.P("@accId", sId));
                    }
                }
            });

            return id;
        }

        public static void LiquidatePartnerAccount(int partnerID, decimal payoutAmount, int safeID, string notes, int userID = 1)
        {
            DbHelper.EnsureFixedAssetsAndShareholdersSchema();

            DbHelper.RunInTransaction((con, trans) =>
            {
                var row = ShareholdersDAL.GetPartnerByID(partnerID);
                string partnerName = row != null ? row["PartnerName"].ToString() : $"شريك #{partnerID}";

                if (payoutAmount > 0)
                {
                    // التحقق من كفاية رصيد الخزينة
                    AccountDAL.EnsureSufficientCashTrans(trans, safeID, payoutAmount, $"تصفية ومستحقات الشريك [{partnerName}]");

                    // قيد صرف نقدية من الخزينة في CashBox
                    DbHelper.ExecuteTrans(trans, @"
                        INSERT INTO CashBox (TransType, AmountOut, RefID, Notes, CreatedBy, AccountID, TransDate)
                        VALUES ('PartnerLiquidation', @amt, @ref, @notes, @uid, @accId, GETDATE())",
                        DbHelper.P("@amt", payoutAmount),
                        DbHelper.P("@ref", partnerID),
                        DbHelper.P("@notes", $"سند صرف تصفية ومستحقات خروج الشريك [{partnerName}]: {notes}"),
                        DbHelper.P("@uid", userID),
                        DbHelper.P("@accId", safeID));

                    // قيد مدين في كشف حساب الشريك
                    DbHelper.ExecuteTrans(trans, @"
                        INSERT INTO PartnerTransactions (PartnerID, TransDate, TransType, Debit, Credit, Notes, SafeID, CreatedBy, CreatedDate)
                        VALUES (@pid, GETDATE(), 'PartnerLiquidation', @amt, 0, @notes, @safe, @uid, GETDATE())",
                        DbHelper.P("@pid", partnerID),
                        DbHelper.P("@amt", payoutAmount),
                        DbHelper.P("@notes", $"صرف مستحقات تصفية نهائية وتخارج: {notes}"),
                        DbHelper.P("@safe", safeID),
                        DbHelper.P("@uid", userID));
                }

                // تصفير حصة الشريك ورأس ماله ورصيده وإلغاء تفعيله
                DbHelper.ExecuteTrans(trans, @"
                    UPDATE Partners
                    SET IsActive = 0,
                        SharePercentage = 0,
                        CapitalContribution = 0,
                        CurrentBalance = 0,
                        Notes = ISNULL(Notes, '') + @n
                    WHERE PartnerID = @pid",
                    DbHelper.P("@pid", partnerID),
                    DbHelper.P("@n", $" [تمت التصفية والتخارج بتاريخ {DateTime.Now:yyyy/MM/dd HH:mm}: {notes}]"));
            });
        }

        public static void DeletePartner(int partnerID)
        {
            DbHelper.EnsureFixedAssetsAndShareholdersSchema();
            DbHelper.Execute("DELETE FROM Partners WHERE PartnerID = @id", DbHelper.P("@id", partnerID));
        }

        // ══════════════════════════════════════════════════
        // 2. كشف حساب وحركات الشريك (Partner Ledger & Transactions)
        // ══════════════════════════════════════════════════
        public static decimal GetPartnerBalance(int partnerID)
        {
            DbHelper.EnsureFixedAssetsAndShareholdersSchema();
            var dt = DbHelper.Query(@"
                SELECT ISNULL(SUM(Credit), 0) - ISNULL(SUM(Debit), 0) AS Balance
                FROM PartnerTransactions
                WHERE PartnerID = @pid", DbHelper.P("@pid", partnerID));
            return dt.Rows.Count > 0 ? Convert.ToDecimal(dt.Rows[0]["Balance"]) : 0m;
        }

        public static DataTable GetPartnerStatement(int partnerID, DateTime? fromDate = null, DateTime? toDate = null)
        {
            DbHelper.EnsureFixedAssetsAndShareholdersSchema();
            string sql = @"
                SELECT pt.*, sa.AccountName AS SafeName, e.EmpName AS UserName
                FROM PartnerTransactions pt
                LEFT JOIN SafeAccounts sa ON pt.SafeID = sa.AccountID
                LEFT JOIN Employees e ON pt.CreatedBy = e.EmpID
                WHERE pt.PartnerID = @pid ";

            var pars = new List<System.Data.SqlClient.SqlParameter> { DbHelper.P("@pid", partnerID) };

            if (fromDate.HasValue)
            {
                sql += " AND pt.TransDate >= @from";
                pars.Add(DbHelper.P("@from", fromDate.Value));
            }
            if (toDate.HasValue)
            {
                sql += " AND pt.TransDate <= @to";
                pars.Add(DbHelper.P("@to", toDate.Value));
            }

            sql += " ORDER BY pt.TransDate ASC, pt.TransID ASC";

            DataTable dt = DbHelper.Query(sql, pars.ToArray());

            // احتساب الرصيد التراكمي
            dt.Columns.Add("RunningBalance", typeof(decimal));
            decimal running = 0m;
            foreach (DataRow r in dt.Rows)
            {
                decimal debit = Convert.ToDecimal(r["Debit"]);
                decimal credit = Convert.ToDecimal(r["Credit"]);
                running += (credit - debit); // دائن (له) موجب، مدين (عليه) سالب
                r["RunningBalance"] = running;
            }

            return dt;
        }

        public static void AddPartnerTransaction(int partnerID, string transType, decimal debit, decimal credit,
            string notes, int? safeID, int? refID, int userID = 1)
        {
            DbHelper.EnsureFixedAssetsAndShareholdersSchema();

            DbHelper.RunInTransaction((con, trans) =>
            {
                // 1. تسجيل الحركة في جدول الشركاء
                DbHelper.ExecuteTrans(trans, @"
                    INSERT INTO PartnerTransactions (PartnerID, TransDate, TransType, Debit, Credit, Notes, SafeID, RefID, CreatedBy, CreatedDate)
                    VALUES (@pid, GETDATE(), @type, @dr, @cr, @notes, @safe, @ref, @uid, GETDATE())",
                    DbHelper.P("@pid", partnerID),
                    DbHelper.P("@type", transType),
                    DbHelper.P("@dr", debit),
                    DbHelper.P("@cr", credit),
                    DbHelper.P("@notes", notes),
                    DbHelper.P("@safe", safeID.HasValue && safeID.Value > 0 ? (object)safeID.Value : DBNull.Value),
                    DbHelper.P("@ref", refID.HasValue ? (object)refID.Value : DBNull.Value),
                    DbHelper.P("@uid", userID));

                // 2. تحديث رصيد الشريك الحالي
                decimal balanceChange = credit - debit;
                DbHelper.ExecuteTrans(trans, @"
                    UPDATE Partners 
                    SET CurrentBalance = CurrentBalance + @chg 
                    WHERE PartnerID = @pid",
                    DbHelper.P("@chg", balanceChange),
                    DbHelper.P("@pid", partnerID));

                // 3. التأثير على الخزينة النقدية (إن وجد حساب خزينة)
                if (safeID.HasValue && safeID.Value > 0)
                {
                    if (credit > 0) // إيداع نقدية من الشريك -> توريد للخزينة
                    {
                        DbHelper.ExecuteTrans(trans, @"
                            INSERT INTO CashBox (TransType, AmountIn, RefID, Notes, CreatedBy, AccountID, TransDate)
                            VALUES ('PartnerDeposit', @amt, @ref, @notes, @uid, @accId, GETDATE())",
                            DbHelper.P("@amt", credit),
                            DbHelper.P("@ref", partnerID),
                            DbHelper.P("@notes", $"إيداع نقدي من الشريك ID:{partnerID}: {notes}"),
                            DbHelper.P("@uid", userID),
                            DbHelper.P("@accId", safeID.Value));
                    }
                    else if (debit > 0) // صرف أرباح أو مسحوبات للشريك -> صرف من الخزينة
                    {
                        DbHelper.ExecuteTrans(trans, @"
                            INSERT INTO CashBox (TransType, AmountOut, RefID, Notes, CreatedBy, AccountID, TransDate)
                            VALUES ('PartnerDrawing', @amt, @ref, @notes, @uid, @accId, GETDATE())",
                            DbHelper.P("@amt", debit),
                            DbHelper.P("@ref", partnerID),
                            DbHelper.P("@notes", $"مسحوبات / صرف أرباح للشريك ID:{partnerID}: {notes}"),
                            DbHelper.P("@uid", userID),
                            DbHelper.P("@accId", safeID.Value));
                    }
                }
            });
        }

        // ══════════════════════════════════════════════════
        // 3. محرك احتساب وتوزيع الأرباح (Dividends Engine)
        // ══════════════════════════════════════════════════
        public static decimal CalculateBusinessNetProfit(DateTime fromDate, DateTime toDate)
        {
            DbHelper.EnsureFixedAssetsAndShareholdersSchema();
            try
            {
                // إجمالي المبيعات
                var salesObj = DbHelper.Scalar(@"
                    SELECT ISNULL(SUM(TotalAmount), 0) 
                    FROM Sales 
                    WHERE IsPosted = 1 AND SaleDate BETWEEN @f AND @t",
                    DbHelper.P("@f", fromDate), DbHelper.P("@t", toDate));
                decimal totalSales = salesObj != null ? Convert.ToDecimal(salesObj) : 0m;

                // تكلفة البضاعة المباعة (أو إجمالي المشتريات)
                var purObj = DbHelper.Scalar(@"
                    SELECT ISNULL(SUM(TotalAmount), 0) 
                    FROM Purchases 
                    WHERE PurchaseDate BETWEEN @f AND @t",
                    DbHelper.P("@f", fromDate), DbHelper.P("@t", toDate));
                decimal totalPurchases = purObj != null ? Convert.ToDecimal(purObj) : 0m;

                // إجمالي المصروفات
                var expObj = DbHelper.Scalar(@"
                    SELECT ISNULL(SUM(Amount), 0) 
                    FROM Expenses 
                    WHERE ExpenseDate BETWEEN @f AND @t",
                    DbHelper.P("@f", fromDate), DbHelper.P("@t", toDate));
                decimal totalExpenses = expObj != null ? Convert.ToDecimal(expObj) : 0m;

                // أرباح التقسيط أو الإيرادات الأخرى
                var otherIncomeObj = DbHelper.Scalar(@"
                    SELECT ISNULL(SUM(AmountIn), 0) 
                    FROM CashBox 
                    WHERE TransType IN ('OtherIncome', 'CapitalIncome') AND TransDate BETWEEN @f AND @t",
                    DbHelper.P("@f", fromDate), DbHelper.P("@t", toDate));
                decimal otherIncome = otherIncomeObj != null ? Convert.ToDecimal(otherIncomeObj) : 0m;

                // صافي الربح التقديري
                decimal netProfit = (totalSales - totalPurchases - totalExpenses) + otherIncome;
                return netProfit;
            }
            catch
            {
                return 0m;
            }
        }

        public static List<DividendDistributionLineDTO> PreviewDividendsDistribution(decimal distributedAmount)
        {
            DbHelper.EnsureFixedAssetsAndShareholdersSchema();
            var list = new List<DividendDistributionLineDTO>();
            var dtPartners = GetAllPartners(true);

            decimal totalAssigned = 0m;
            int count = dtPartners.Rows.Count;
            int i = 0;

            foreach (DataRow r in dtPartners.Rows)
            {
                i++;
                int pid = Convert.ToInt32(r["PartnerID"]);
                string name = r["PartnerName"].ToString();
                decimal pct = Convert.ToDecimal(r["SharePercentage"]);

                decimal partnerShare = Math.Round(distributedAmount * (pct / 100m), 2);
                if (i == count && count > 0)
                {
                    // تسوية أي كسور
                    partnerShare = distributedAmount - totalAssigned;
                }
                totalAssigned += partnerShare;

                list.Add(new DividendDistributionLineDTO
                {
                    PartnerID = pid,
                    PartnerName = name,
                    SharePercentage = pct,
                    CalculatedProfit = partnerShare,
                    IsPaid = false,
                    PaidAmount = 0
                });
            }

            return list;
        }

        public static int PostDividendsDistribution(DateTime fromDate, DateTime toDate, decimal netProfit,
            decimal retainedPct, decimal distributedAmount, List<DividendDistributionLineDTO> lines, string notes, int userID = 1)
        {
            DbHelper.EnsureFixedAssetsAndShareholdersSchema();
            int distID = 0;

            DbHelper.RunInTransaction((con, trans) =>
            {
                // 1. إنشاء سجل جلسة التوزيع
                distID = DbHelper.ExecuteInsertTrans(trans, @"
                    INSERT INTO DividendDistributions (DistributionDate, PeriodFrom, PeriodTo, NetBusinessProfit,
                                                        RetainedProfitPct, DistributedProfitAmount, Notes, CreatedBy, CreatedDate)
                    VALUES (GETDATE(), @f, @t, @np, @rp, @dist, @notes, @uid, GETDATE())",
                    DbHelper.P("@f", fromDate),
                    DbHelper.P("@t", toDate),
                    DbHelper.P("@np", netProfit),
                    DbHelper.P("@rp", retainedPct),
                    DbHelper.P("@dist", distributedAmount),
                    DbHelper.P("@notes", notes),
                    DbHelper.P("@uid", userID));

                // 2. إدراج سطور التوزيع وقيد استحقاق الأرباح في كشف حساب كل شريك
                foreach (var line in lines)
                {
                    int lineID = DbHelper.ExecuteInsertTrans(trans, @"
                        INSERT INTO DividendDistributionLines (DistributionID, PartnerID, SharePercentage, CalculatedProfit, IsPaid, PaidAmount)
                        VALUES (@did, @pid, @pct, @amt, 0, 0)",
                        DbHelper.P("@did", distID),
                        DbHelper.P("@pid", line.PartnerID),
                        DbHelper.P("@pct", line.SharePercentage),
                        DbHelper.P("@amt", line.CalculatedProfit));

                    // قيد استحقاق أرباح (له / Credit) في كشف حساب الشريك
                    DbHelper.ExecuteTrans(trans, @"
                        INSERT INTO PartnerTransactions (PartnerID, TransDate, TransType, Debit, Credit, Notes, RefID, CreatedBy, CreatedDate)
                        VALUES (@pid, GETDATE(), 'ProfitShare', 0, @amt, @n, @ref, @uid, GETDATE())",
                        DbHelper.P("@pid", line.PartnerID),
                        DbHelper.P("@amt", line.CalculatedProfit),
                        DbHelper.P("@n", $"استحقاق أرباح فترة [{fromDate:yyyy/MM/dd} إلى {toDate:yyyy/MM/dd}] (حصة {line.SharePercentage}%)"),
                        DbHelper.P("@ref", distID),
                        DbHelper.P("@uid", userID));

                    // تحديث رصيد الشريك
                    DbHelper.ExecuteTrans(trans, @"
                        UPDATE Partners 
                        SET CurrentBalance = CurrentBalance + @amt 
                        WHERE PartnerID = @pid",
                        DbHelper.P("@amt", line.CalculatedProfit),
                        DbHelper.P("@pid", line.PartnerID));
                }
            });

            return distID;
        }

        public static void DisbursePartnerDividends(int lineID, int partnerID, decimal amount, int safeID, string notes, int userID = 1)
        {
            DbHelper.EnsureFixedAssetsAndShareholdersSchema();

            DbHelper.RunInTransaction((con, trans) =>
            {
                // التحقق من كفاية رصيد الخزينة
                AccountDAL.EnsureSufficientCashTrans(trans, safeID, amount, "صرف أرباح الشريك");

                // 1. تسجيل صرف الأرباح في كشف حساب الشريك (عليه / Debit)
                DbHelper.ExecuteTrans(trans, @"
                    INSERT INTO PartnerTransactions (PartnerID, TransDate, TransType, Debit, Credit, Notes, SafeID, RefID, CreatedBy, CreatedDate)
                    VALUES (@pid, GETDATE(), 'DividendPayout', @amt, 0, @notes, @safe, @ref, @uid, GETDATE())",
                    DbHelper.P("@pid", partnerID),
                    DbHelper.P("@amt", amount),
                    DbHelper.P("@notes", string.IsNullOrWhiteSpace(notes) ? "صرف نصيب أرباح نقدياً من الخزينة" : notes),
                    DbHelper.P("@safe", safeID),
                    DbHelper.P("@ref", lineID),
                    DbHelper.P("@uid", userID));

                // 2. تحديث سطر التوزيع
                if (lineID > 0)
                {
                    DbHelper.ExecuteTrans(trans, @"
                        UPDATE DividendDistributionLines
                        SET IsPaid = 1, PaidAmount = PaidAmount + @amt, PaidDate = GETDATE(), PaidSafeID = @safe
                        WHERE LineID = @lid",
                        DbHelper.P("@amt", amount),
                        DbHelper.P("@safe", safeID),
                        DbHelper.P("@lid", lineID));
                }

                // 3. خصم من رصيد الشريك
                DbHelper.ExecuteTrans(trans, @"
                    UPDATE Partners 
                    SET CurrentBalance = CurrentBalance - @amt 
                    WHERE PartnerID = @pid",
                    DbHelper.P("@amt", amount),
                    DbHelper.P("@pid", partnerID));

                // 4. خصم نقدية من الخزينة
                DbHelper.ExecuteTrans(trans, @"
                    INSERT INTO CashBox (TransType, AmountOut, RefID, Notes, CreatedBy, AccountID, TransDate)
                    VALUES ('PartnerDividend', @amt, @ref, @notes, @uid, @accId, GETDATE())",
                    DbHelper.P("@amt", amount),
                    DbHelper.P("@ref", partnerID),
                    DbHelper.P("@notes", $"صرف أرباح نقدية للشريك ID:{partnerID}: {notes}"),
                    DbHelper.P("@uid", userID),
                    DbHelper.P("@accId", safeID));
            });
        }

        public static void DisburseAllDividendsFromSafe(int distributionID, int safeID, string notes, int userID = 1)
        {
            DbHelper.EnsureFixedAssetsAndShareholdersSchema();

            DbHelper.RunInTransaction((con, trans) =>
            {
                var dt = DbHelper.QueryTrans(trans, @"
                    SELECT ddl.LineID, ddl.PartnerID, ddl.CalculatedProfit, p.PartnerName
                    FROM DividendDistributionLines ddl
                    JOIN Partners p ON ddl.PartnerID = p.PartnerID
                    WHERE ddl.DistributionID = @did AND (ddl.IsPaid = 0 OR ddl.IsPaid IS NULL)",
                    DbHelper.P("@did", distributionID));

                if (dt.Rows.Count == 0) return;

                decimal totalAmount = 0m;
                foreach (DataRow r in dt.Rows)
                    totalAmount += Convert.ToDecimal(r["CalculatedProfit"]);

                // التحقق من كفاية رصيد الخزينة لإجمالي المبالغ الموزعة
                AccountDAL.EnsureSufficientCashTrans(trans, safeID, totalAmount, "صرف أرباح الشركاء الموزعة");

                foreach (DataRow r in dt.Rows)
                {
                    int lineID = Convert.ToInt32(r["LineID"]);
                    int partnerID = Convert.ToInt32(r["PartnerID"]);
                    decimal profit = Convert.ToDecimal(r["CalculatedProfit"]);
                    string pName = r["PartnerName"].ToString();

                    // 1. تسجيل صرف الأرباح في كشف حساب الشريك (مدين / Debit)
                    DbHelper.ExecuteTrans(trans, @"
                        INSERT INTO PartnerTransactions (PartnerID, TransDate, TransType, Debit, Credit, Notes, SafeID, RefID, CreatedBy, CreatedDate)
                        VALUES (@pid, GETDATE(), 'DividendPayout', @amt, 0, @notes, @safe, @ref, @uid, GETDATE())",
                        DbHelper.P("@pid", partnerID),
                        DbHelper.P("@amt", profit),
                        DbHelper.P("@notes", string.IsNullOrWhiteSpace(notes) ? $"صرف أرباح جلسة #{distributionID} نقداً للشريك [{pName}]" : notes),
                        DbHelper.P("@safe", safeID),
                        DbHelper.P("@ref", lineID),
                        DbHelper.P("@uid", userID));

                    // 2. تحديث سطر التوزيع
                    DbHelper.ExecuteTrans(trans, @"
                        UPDATE DividendDistributionLines
                        SET IsPaid = 1, PaidAmount = @amt, PaidDate = GETDATE(), PaidSafeID = @safe
                        WHERE LineID = @lid",
                        DbHelper.P("@amt", profit),
                        DbHelper.P("@safe", safeID),
                        DbHelper.P("@lid", lineID));

                    // 3. خصم من رصيد الشريك الجاري
                    DbHelper.ExecuteTrans(trans, @"
                        UPDATE Partners 
                        SET CurrentBalance = CurrentBalance - @amt 
                        WHERE PartnerID = @pid",
                        DbHelper.P("@amt", profit),
                        DbHelper.P("@pid", partnerID));

                    // 4. خصم النقدية من الخزينة في CashBox
                    DbHelper.ExecuteTrans(trans, @"
                        INSERT INTO CashBox (TransType, AmountOut, RefID, Notes, CreatedBy, AccountID, TransDate)
                        VALUES ('PartnerDividend', @amt, @ref, @notes, @uid, @accId, GETDATE())",
                        DbHelper.P("@amt", profit),
                        DbHelper.P("@ref", partnerID),
                        DbHelper.P("@notes", $"صرف أرباح نقدية للشريك [{pName}] (جلسة #{distributionID})"),
                        DbHelper.P("@uid", userID),
                        DbHelper.P("@accId", safeID));
                }
            });
        }

        public static DataTable GetDistributionHistory()
        {
            DbHelper.EnsureFixedAssetsAndShareholdersSchema();
            return DbHelper.Query(@"
                SELECT dd.*, e.EmpName AS CreatedByName
                FROM DividendDistributions dd
                LEFT JOIN Employees e ON dd.CreatedBy = e.EmpID
                ORDER BY dd.DistributionDate DESC");
        }

        public static DataTable GetDistributionLines(int distributionID)
        {
            DbHelper.EnsureFixedAssetsAndShareholdersSchema();
            return DbHelper.Query(@"
                SELECT ddl.*, p.PartnerName, sa.AccountName AS SafeName
                FROM DividendDistributionLines ddl
                JOIN Partners p ON ddl.PartnerID = p.PartnerID
                LEFT JOIN SafeAccounts sa ON ddl.PaidSafeID = sa.AccountID
                WHERE ddl.DistributionID = @did
                ORDER BY ddl.LineID ASC", DbHelper.P("@did", distributionID));
        }

        public static (decimal totalCapital, int partnerCount, decimal totalDrawings, decimal totalDistributed) GetSummaryMetrics()
        {
            DbHelper.EnsureFixedAssetsAndShareholdersSchema();
            var dtCap = DbHelper.Query(@"
                SELECT 
                    ISNULL(SUM(CapitalContribution), 0) AS TotalCap,
                    COUNT(*) AS PartnerCount
                FROM Partners
                WHERE IsActive = 1");

            var dtTrans = DbHelper.Query(@"
                SELECT 
                    ISNULL(SUM(CASE WHEN TransType IN ('PersonalDrawing', 'DividendPayout') THEN Debit ELSE 0 END), 0) AS TotalDrawings,
                    ISNULL(SUM(CASE WHEN TransType = 'ProfitShare' THEN Credit ELSE 0 END), 0) AS TotalProfits
                FROM PartnerTransactions");

            decimal cap = dtCap.Rows.Count > 0 ? Convert.ToDecimal(dtCap.Rows[0]["TotalCap"]) : 0m;
            int count = dtCap.Rows.Count > 0 ? Convert.ToInt32(dtCap.Rows[0]["PartnerCount"]) : 0;
            decimal drawings = dtTrans.Rows.Count > 0 ? Convert.ToDecimal(dtTrans.Rows[0]["TotalDrawings"]) : 0m;
            decimal profits = dtTrans.Rows.Count > 0 ? Convert.ToDecimal(dtTrans.Rows[0]["TotalProfits"]) : 0m;

            return (cap, count, drawings, profits);
        }
    }
}
